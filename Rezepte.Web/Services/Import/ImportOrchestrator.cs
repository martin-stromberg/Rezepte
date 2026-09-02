using Microsoft.Extensions.DependencyInjection;
using Rezepte.Web.Services.Import.Plugins;
using System.Collections.Concurrent;

namespace Rezepte.Web.Services.Import;

public sealed class ImportOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<ImportOrchestrator> _logger;

    // Simple in-memory sessions (replaceable with distributed store)
    private readonly ConcurrentDictionary<string, ImportSession> _sessions = new();

    public ImportOrchestrator(IServiceScopeFactory scopeFactory, IPluginManager pluginManager, ILogger<ImportOrchestrator> logger)
    {
        _scopeFactory = scopeFactory;
        _pluginManager = pluginManager;
        _logger = logger;
    }

    public record ImportSession(string Id, string InitiatorUserId)
    {
        internal object SyncRoot { get; } = new();
        public string Status { get; set; } = "Queued";
        public string State { get; set; } = "Queued";
        public bool ReadOnly { get; set; }
        public bool WaitingForConfirmation { get; set; } = false;
        public string? ConfirmationPrompt { get; set; }
        public TaskCompletionSource<bool>? ConfirmationTcs { get; set; }
        public TaskCompletionSource<ImportCollectionSelection>? SelectionTcs { get; set; }
        public ImportCollectionPreview? CollectionPreview { get; set; }
        public List<ImportCollectionItemStatus> CollectionItems { get; set; } = [];
        public ImportResult? Result { get; set; }
    }

    public ImportSession? GetSessionForUser(string id, string userId) => TryGetSessionForUser(id, userId, out var session) ? session : null;

    public async Task<string> StartImportAsync(Stream stream, string fileName, string? uri, string? targetCookbookId, string userId, CancellationToken ct = default)
    {
        // Make an independent in-memory copy of the provided stream so background processing
        // does not depend on the caller keeping the original stream open.
        var workStream = new MemoryStream();
        try
        {
            // Copy the source stream into a private memory stream (respect cancellation)
            await stream.CopyToAsync(workStream, ct).ConfigureAwait(false);
            workStream.Seek(0, SeekOrigin.Begin);
        }
        catch
        {
            workStream.Dispose();
            throw;
        }

        var sessionId = Guid.NewGuid().ToString("n");
        var session = new ImportSession(sessionId, userId);
        _sessions[sessionId] = session;

        // Run background processing (fire and forget) without passing the request token to Task.Run:
        // an already cancelled token would prevent the delegate from running at all, leaving the
        // session stuck in its initial state and leaking the working stream. Cancellation is still
        // observed by the operations inside the delegate.
        _ = Task.Run(async () =>
        {
            // ensure we dispose the private copy when work is done
            try
            {
                // create a scope so handlers (which are scoped) can be resolved safely from this singleton orchestrator
                using var scope = _scopeFactory.CreateScope();
                await using var lease = await _pluginManager.AcquireActiveHandlersAsync(scope.ServiceProvider, ct).ConfigureAwait(false);
                List<Exception> errors = new List<Exception>();

                session.Status = "Starting";
                session.State = "Checking";
                foreach (var pluginHandler in lease.Handlers)
                {
                    var handler = pluginHandler.Handler;
                    ct.ThrowIfCancellationRequested();
                    handler.UserId = userId;
                    session.Status = $"Checking plugin {pluginHandler.Plugin.DisplayName}";
                    session.State = "Checking";

                    if (handler is ICollectionImportHandler collectionHandler)
                    {
                        if (workStream.CanSeek) workStream.Seek(0, SeekOrigin.Begin);
                        ImportCollectionPreview? preview = null;
                        try
                        {
                            preview = await collectionHandler.TryReadCollectionPreviewAsync(workStream, fileName, uri, ct).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Plugin {PluginId} handler {Handler} collection preview failed", pluginHandler.Plugin.Id, handler.GetType().Name);
                        }

                        if (preview is not null)
                        {
                            var persister = scope.ServiceProvider.GetRequiredService<IImportedRecipePersister>();
                            await HandleCollectionImportAsync(session, collectionHandler, preview, persister, userId, ct).ConfigureAwait(false);
                            break;
                        }
                    }

                    // reset stream position for each handler
                    if (workStream.CanSeek) workStream.Seek(0, SeekOrigin.Begin);

                    bool can;
                    try
                    {
                        can = await handler.CanHandleAsync(workStream, fileName, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Plugin {PluginId} handler {Handler} CanHandleAsync failed", pluginHandler.Plugin.Id, handler.GetType().Name);
                        continue;
                    }

                    if (!can) continue;

                    session.Status = $"Handling with {pluginHandler.Plugin.DisplayName}";
                    session.State = "Importing";
                    try
                    {
                        // If handler supports interactive API, call it with interaction implementation
                        if (handler is IInteractiveImportHandler interactive)
                        {
                            // create interaction that ties into the session
                            var interaction = new SessionInteraction(session, _logger);
                            // note: pass fresh stream (seek to 0)
                            if (workStream.CanSeek) workStream.Seek(0, SeekOrigin.Begin);
                            // The plugin contract (Rezepte.Import.Abstractions.IInteractiveImportHandler)
                            // requires a non-nullable targetCookbookId; an absent target is
                            // represented as "" there, matching RecipeService.CreateAsync's
                            // treatment of blank cookbook ids as "no cookbook assigned".
                            var res = await interactive.HandleInteractiveAsync(workStream, fileName, uri, targetCookbookId ?? string.Empty, userId, interaction, ct).ConfigureAwait(false);
                            res = await scope.ServiceProvider.GetRequiredService<IImportedRecipePersister>()
                                .PersistAsync(res, targetCookbookId, userId, ct)
                                .ConfigureAwait(false);
                            session.Result = res;
                            session.Status = res.Success ? "Completed" : "Failed: " + res.Error;
                            session.State = res.Success ? "Completed" : "Failed";
                            break;
                        }
                        else
                        {
                            if (workStream.CanSeek) workStream.Seek(0, SeekOrigin.Begin);
                            // See the HandleInteractiveAsync call above: the plugin contract needs a
                            // non-nullable targetCookbookId.
                            var res = await handler.HandleAsync(workStream, fileName, uri, targetCookbookId ?? string.Empty, userId, ct).ConfigureAwait(false);
                            res = await scope.ServiceProvider.GetRequiredService<IImportedRecipePersister>()
                                .PersistAsync(res, targetCookbookId, userId, ct)
                                .ConfigureAwait(false);
                            session.Result = res;
                            session.Status = res.Success ? "Completed" : "Failed: " + res.Error;
                            session.State = res.Success ? "Completed" : "Failed";
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                        _logger.LogWarning(ex, "Plugin {PluginId} handler {Handler} failed during import", pluginHandler.Plugin.Id, handler.GetType().Name);
                        session.Result = new ImportResult(false, ImportExceptionHelper.BeautifyExceptionMessage(ex), new List<string>());
                        session.Status = "Failed: " + session.Result.Error;
                        session.State = "Failed";
                        break;
                    }
                }
                if (session.Result == null)
                    if (errors.Any())
                    {
                        session.Result = new ImportResult(false, $"Handlers accepting the file were found, but ended in exception. {string.Join("\r\n", errors.Select(e => ImportExceptionHelper.BeautifyExceptionMessage(e)))}", new List<string>());
                        session.Status = "No suitable plugin found";
                        session.State = "Failed";
                    }
                    else
                    {
                        session.Result = new ImportResult(false, "No suitable import plugin found for this file or URL.", new List<string>());
                        session.Status = "No suitable plugin found";
                        session.State = "Failed";
                    }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Import session {Session} was cancelled", sessionId);
                session.Status = "Cancelled";
                session.State = "Failed";
                session.Result = new ImportResult(false, "Cancelled", new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import session {Session} failed", sessionId);
                session.Status = "Error: " + ex.Message;
                session.State = "Failed";
                session.Result = new ImportResult(false, ex.Message, new List<string>());
            }
            finally
            {
                // dispose the private copy of the stream
                try
                {
                    workStream.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not dispose the working stream of import session {Session}", sessionId);
                }
            }
        });

        return sessionId;
    }

    // Called by UI to confirm a waiting session
    public bool Confirm(string sessionId, string userId, bool accepted)
    {
        if (!TryGetSessionForUser(sessionId, userId, out var session) || session.ConfirmationTcs == null)
            return false;
        session.WaitingForConfirmation = false;
        session.ConfirmationTcs.TrySetResult(accepted);
        return true;
    }

    public SelectionSubmitResult SubmitSelection(string sessionId, string userId, ImportCollectionSelection selection)
    {
        if (!TryGetSessionForUser(sessionId, userId, out var session))
        {
            return SelectionSubmitResult.NotFound("Import-Session wurde nicht gefunden.");
        }

        lock (session.SyncRoot)
        {
            if (session.State != "SelectionRequired" || session.SelectionTcs is null || session.CollectionPreview is null)
            {
                return SelectionSubmitResult.Invalid("Die Import-Session erwartet keine Auswahl.");
            }

            if (selection.Items.Count == 0)
            {
                return SelectionSubmitResult.Invalid("Es muss mindestens ein Rezept ausgewählt werden.");
            }

            if (!TryCreatePreviewLookup(session.CollectionPreview, out var previewItems))
            {
                return SelectionSubmitResult.Invalid("Die Sammlung enthält doppelte Rezept-IDs und kann nicht importiert werden.");
            }

            foreach (var item in selection.Items)
            {
                if (string.IsNullOrWhiteSpace(item.TargetCookbookId))
                {
                    return SelectionSubmitResult.Invalid("Für jedes ausgewählte Rezept muss ein Zielkochbuch gesetzt sein.");
                }

                if (!previewItems.TryGetValue(item.ItemId, out var previewItem))
                {
                    return SelectionSubmitResult.Invalid($"Das Rezept {item.ItemId} ist nicht Teil der Sammlung.");
                }

                if (!string.Equals(previewItem.Url, item.Url, StringComparison.OrdinalIgnoreCase))
                {
                    return SelectionSubmitResult.Invalid($"Die URL für Rezept {item.ItemId} passt nicht zur Vorschau.");
                }
            }

            if (!session.SelectionTcs.TrySetResult(selection))
            {
                return SelectionSubmitResult.Invalid("Die Auswahl wurde bereits verarbeitet.");
            }

            session.ReadOnly = true;
            session.State = "Importing";
            session.Status = "Importiere ausgewählte Rezepte";
            return SelectionSubmitResult.Accepted();
        }
    }

    public SelectionSubmitResult CancelSelection(string sessionId, string userId)
    {
        if (!TryGetSessionForUser(sessionId, userId, out var session))
        {
            return SelectionSubmitResult.NotFound("Import-Session wurde nicht gefunden.");
        }

        lock (session.SyncRoot)
        {
            if (session.State != "SelectionRequired" || session.SelectionTcs is null)
            {
                return SelectionSubmitResult.Invalid("Die Import-Session erwartet keine Auswahl.");
            }

            if (!session.SelectionTcs.TrySetCanceled())
            {
                return SelectionSubmitResult.Invalid("Die Auswahl wurde bereits verarbeitet.");
            }

            session.ReadOnly = true;
            session.Status = "Cancelled";
            session.State = "Failed";
            session.Result = new ImportResult(false, "Import cancelled.", []);
            return SelectionSubmitResult.Accepted();
        }
    }

    private bool TryGetSessionForUser(string sessionId, string userId, out ImportSession session)
    {
        if (_sessions.TryGetValue(sessionId, out var found)
            && string.Equals(found.InitiatorUserId, userId, StringComparison.Ordinal))
        {
            session = found;
            return true;
        }

        session = null!;
        return false;
    }

    private async Task HandleCollectionImportAsync(
        ImportSession session,
        ICollectionImportHandler handler,
        ImportCollectionPreview preview,
        IImportedRecipePersister persister,
        string userId,
        CancellationToken ct)
    {
        if (!TryCreatePreviewLookup(preview, out var initialPreviewById))
        {
            session.ReadOnly = true;
            session.Status = "Failed: Die Sammlung enthält doppelte Rezept-IDs.";
            session.State = "Failed";
            session.Result = new ImportResult(false, "Die Sammlung enthält doppelte Rezept-IDs.", []);
            return;
        }

        session.CollectionPreview = preview;
        session.CollectionItems = preview.Items
            .Select(item => new ImportCollectionItemStatus(item.Id, item.Title, item.Url, string.Empty, ImportCollectionItemState.Pending))
            .ToList();
        session.SelectionTcs = new TaskCompletionSource<ImportCollectionSelection>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Status = "Auswahl erforderlich";
        session.State = "SelectionRequired";

        if (ct.CanBeCanceled)
        {
            ct.Register(() => session.SelectionTcs.TrySetCanceled(ct));
        }

        var selection = await session.SelectionTcs.Task.ConfigureAwait(false);
        session.ReadOnly = true;
        session.State = "Importing";
        session.Status = "Importiere ausgewählte Rezepte";

        session.CollectionItems = selection.Items
            .Select(selected =>
            {
                var previewItem = initialPreviewById[selected.ItemId];
                return new ImportCollectionItemStatus(
                    selected.ItemId,
                    previewItem.Title,
                    previewItem.Url,
                    selected.TargetCookbookId,
                    ImportCollectionItemState.Pending);
            })
            .ToList();

        var createdIds = new List<string>();
        foreach (var selected in selection.Items)
        {
            ct.ThrowIfCancellationRequested();
            var previewItem = initialPreviewById[selected.ItemId];
            SetCollectionItemStatus(session, selected.ItemId, ImportCollectionItemState.Importing, selected.TargetCookbookId);
            session.Status = $"Importiere {previewItem.Title}";

            try
            {
                var importResult = await handler.ImportCollectionItemAsync(previewItem, userId, ct).ConfigureAwait(false);
                if (!importResult.Success || importResult.ImportedRecipes is null || importResult.ImportedRecipes.Count == 0)
                {
                    SetCollectionItemStatus(session, selected.ItemId, ImportCollectionItemState.Failed, selected.TargetCookbookId, importResult.Error ?? "Das Rezept konnte nicht importiert werden.");
                    continue;
                }

                string? recipeId = null;
                foreach (var imported in importResult.ImportedRecipes)
                {
                    var persisted = await persister
                        .PersistRecipeAsync(imported, selected.TargetCookbookId, userId, ct)
                        .ConfigureAwait(false);
                    if (!persisted.Success)
                    {
                        SetCollectionItemStatus(session, selected.ItemId, ImportCollectionItemState.Failed, selected.TargetCookbookId, persisted.Error ?? "Das Rezept konnte nicht gespeichert werden.");
                        recipeId = null;
                        break;
                    }

                    recipeId ??= persisted.RecipeId;
                    if (!string.IsNullOrWhiteSpace(persisted.RecipeId))
                    {
                        createdIds.Add(persisted.RecipeId);
                    }
                }

                if (recipeId is not null)
                {
                    SetCollectionItemStatus(session, selected.ItemId, ImportCollectionItemState.Succeeded, selected.TargetCookbookId, recipeId: recipeId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Collection item {ItemId} failed", selected.ItemId);
                SetCollectionItemStatus(session, selected.ItemId, ImportCollectionItemState.Failed, selected.TargetCookbookId, ImportExceptionHelper.BeautifyExceptionMessage(ex));
            }
        }

        var failed = session.CollectionItems.Count(i => i.State == ImportCollectionItemState.Failed);
        var succeeded = session.CollectionItems.Count(i => i.State == ImportCollectionItemState.Succeeded);
        session.Result = succeeded > 0
            ? new ImportResult(true, failed > 0 ? $"{failed} Rezept(e) konnten nicht importiert werden." : null, createdIds)
            : new ImportResult(false, "Keines der ausgewählten Rezepte konnte importiert werden.", createdIds);
        session.Status = succeeded > 0 ? "Completed" : "Failed: Keines der ausgewählten Rezepte konnte importiert werden.";
        session.State = succeeded > 0 ? "Completed" : "Failed";
    }

    private static bool TryCreatePreviewLookup(
        ImportCollectionPreview preview,
        out Dictionary<string, ImportCollectionItem> previewById)
    {
        previewById = new Dictionary<string, ImportCollectionItem>(StringComparer.Ordinal);
        foreach (var item in preview.Items)
        {
            if (!previewById.TryAdd(item.Id, item))
            {
                previewById.Clear();
                return false;
            }
        }

        return true;
    }

    private static void SetCollectionItemStatus(
        ImportSession session,
        string itemId,
        ImportCollectionItemState state,
        string targetCookbookId,
        string? error = null,
        string? recipeId = null)
    {
        var index = session.CollectionItems.FindIndex(i => i.ItemId == itemId);
        if (index < 0)
        {
            return;
        }

        var current = session.CollectionItems[index];
        session.CollectionItems[index] = current with
        {
            State = state,
            TargetCookbookId = targetCookbookId,
            Error = error,
            RecipeId = recipeId
        };
    }

    public sealed record SelectionSubmitResult(bool Success, bool IsNotFound, string? Error)
    {
        public static SelectionSubmitResult Accepted() => new(true, false, null);
        public static SelectionSubmitResult Invalid(string error) => new(false, false, error);
        public static SelectionSubmitResult NotFound(string error) => new(false, true, error);
    }

    private class SessionInteraction : IImportInteraction
    {
        private readonly ImportSession _session;
        private readonly ILogger _logger;
        public SessionInteraction(ImportSession session, ILogger logger)
        {
            _session = session;
            _logger = logger;
        }

        public Task ReportStatusAsync(string status, CancellationToken ct = default)
        {
            _session.Status = status;
            return Task.CompletedTask;
        }

        public Task<bool> AskForConfirmationAsync(string prompt, CancellationToken ct = default)
        {
            _session.ConfirmationPrompt = prompt;
            _session.WaitingForConfirmation = true;
            _session.ConfirmationTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // optionally: register cancellation to set TCS canceled
            if (ct.CanBeCanceled)
            {
                ct.Register(() => _session.ConfirmationTcs.TrySetCanceled());
            }

            _logger.LogInformation("Session {Session} asks for confirmation: {Prompt}", _session.Id, prompt);
            return _session.ConfirmationTcs.Task;
        }
    }
}
