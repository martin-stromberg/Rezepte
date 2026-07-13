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

    public record ImportSession(string Id)
    {
        public string Status { get; set; } = "Queued";
        public bool WaitingForConfirmation { get; set; } = false;
        public string? ConfirmationPrompt { get; set; }
        public TaskCompletionSource<bool>? ConfirmationTcs { get; set; }
        public ImportResult? Result { get; set; }
    }

    public ImportSession? GetSession(string id) => _sessions.TryGetValue(id, out var s) ? s : null;

    public async Task<string> StartImportAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
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
        var session = new ImportSession(sessionId);
        _sessions[sessionId] = session;

        // run background processing (fire-and-wait pattern but record session)
        _ = Task.Run(async () =>
        {
            // ensure we dispose the private copy when work is done
            try
            {
                // create a scope so handlers (which are scoped) can be resolved safely from this singleton orchestrator
                using var scope = _scopeFactory.CreateScope();
                var handlers = await _pluginManager.GetActiveHandlersAsync(scope.ServiceProvider, ct).ConfigureAwait(false);
                List<Exception> errors = new List<Exception>();

                session.Status = "Starting";
                foreach (var pluginHandler in handlers)
                {
                    var handler = pluginHandler.Handler;
                    ct.ThrowIfCancellationRequested();
                    handler.UserId = userId;
                    session.Status = $"Checking plugin {pluginHandler.Plugin.DisplayName}";

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
                    try { 
                    // If handler supports interactive API, call it with interaction implementation
                    if (handler is IInteractiveImportHandler interactive)
                    {
                        // create interaction that ties into the session
                        var interaction = new SessionInteraction(session, _logger);
                        // note: pass fresh stream (seek to 0)
                        if (workStream.CanSeek) workStream.Seek(0, SeekOrigin.Begin);
                        var res = await interactive.HandleInteractiveAsync(workStream, fileName, uri, targetCookbookId, userId, interaction, ct).ConfigureAwait(false);
                        res = await scope.ServiceProvider.GetRequiredService<IImportedRecipePersister>()
                            .PersistAsync(res, targetCookbookId, userId, ct)
                            .ConfigureAwait(false);
                        session.Result = res;
                        session.Status = res.Success ? "Completed" : "Failed: " + res.Error;
                        break;
                    }
                    else
                    {
                        if (workStream.CanSeek) workStream.Seek(0, SeekOrigin.Begin);
                        var res = await handler.HandleAsync(workStream, fileName, uri, targetCookbookId, userId, ct).ConfigureAwait(false);
                        res = await scope.ServiceProvider.GetRequiredService<IImportedRecipePersister>()
                            .PersistAsync(res, targetCookbookId, userId, ct)
                            .ConfigureAwait(false);
                        session.Result = res;
                        session.Status = res.Success ? "Completed" : "Failed: " + res.Error;
                        break;
                    }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                        _logger.LogWarning(ex, "Plugin {PluginId} handler {Handler} failed during import", pluginHandler.Plugin.Id, handler.GetType().Name);
                        session.Result = new ImportResult(false, ImportExceptionHelper.BeautifyExceptionMessage(ex), new List<string>());
                        session.Status = "Failed: " + session.Result.Error;
                        break;
                    }
                }
                if (session.Result == null)
                    if (errors.Any())
                    {
                        session.Result = new ImportResult(false, $"Handlers accepting the file were found, but ended in exception. {string.Join("\r\n", errors.Select(e => ImportExceptionHelper.BeautifyExceptionMessage(e)))}", new List<string>());
                        session.Status = "No suitable plugin found";
                    }
                    else
                    {
                        session.Result = new ImportResult(false, "No suitable import plugin found for this file or URL.", new List<string>());
                        session.Status = "No suitable plugin found";
                    }
            }
            catch (OperationCanceledException)
            {
                session.Status = "Cancelled";
                session.Result = new ImportResult(false, "Cancelled", new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import session {Session} failed", sessionId);
                session.Status = "Error: " + ex.Message;
                session.Result = new ImportResult(false, ex.Message, new List<string>());
            }
            finally
            {
                // dispose the private copy of the stream
                try { workStream.Dispose(); } catch { /* swallow */ }
            }
        }, ct);

        return sessionId;
    }

    // Called by UI to confirm a waiting session
    public bool Confirm(string sessionId, bool accepted)
    {
        if (!_sessions.TryGetValue(sessionId, out var session) || session.ConfirmationTcs == null)
            return false;
        session.WaitingForConfirmation = false;
        session.ConfirmationTcs.TrySetResult(accepted);
        return true;
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
