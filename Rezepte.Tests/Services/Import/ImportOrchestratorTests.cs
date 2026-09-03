using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Services.Import;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class ImportOrchestratorTests
{
    private const string OwnerUserId = "user-1";
    private const string OtherUserId = "user-2";

    [Fact]
    public async Task StartImportAsync_ShouldUseActivePluginsInConfiguredOrder()
    {
        var first = new RecordingHandler("first", canHandle: false);
        var second = new RecordingHandler("second", canHandle: true);
        var sut = CreateOrchestrator(first, second);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1, 2, 3]), "recipe.fixture", null, "cookbook-1", OwnerUserId);

        var session = await WaitForResultAsync(sut, sessionId);
        session.Result!.Success.Should().BeTrue();
        session.Result.CreatedRecipeIds.Should().Equal("second-created");
        first.CanHandleCalls.Should().Be(1);
        second.CanHandleCalls.Should().Be(1);
        second.HandleCalls.Should().Be(1);
    }

    [Fact]
    public async Task StartImportAsync_ShouldStoreInitiatorUserId()
    {
        var sut = CreateOrchestrator(new RecordingHandler("handler", canHandle: true));

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", OwnerUserId);

        var session = await WaitForResultAsync(sut, sessionId);
        session.InitiatorUserId.Should().Be(OwnerUserId);
        sut.GetSessionForUser(sessionId, OwnerUserId).Should().BeSameAs(session);
    }

    [Fact]
    public async Task GetSessionForUser_ShouldHideSessionFromDifferentUser()
    {
        var sut = CreateOrchestrator(new RecordingHandler("handler", canHandle: true));

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", OwnerUserId);
        await WaitForResultAsync(sut, sessionId);

        sut.GetSessionForUser(sessionId, OtherUserId).Should().BeNull();
    }

    [Fact]
    public async Task StartImportAsync_ShouldSupportInteractivePluginConfirmation()
    {
        var interactive = new InteractiveRecordingHandler("interactive", canHandle: true);
        var sut = CreateOrchestrator(interactive);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", OwnerUserId);
        var waitingSession = await WaitUntilAsync(sut, sessionId, s => s.WaitingForConfirmation);

        waitingSession.ConfirmationPrompt.Should().Be("Import recipe?");
        sut.Confirm(sessionId, OwnerUserId, accepted: true).Should().BeTrue();

        var completedSession = await WaitForResultAsync(sut, sessionId);
        completedSession.Result!.Success.Should().BeTrue();
        completedSession.Result.CreatedRecipeIds.Should().Equal("interactive-confirmed");
    }

    [Fact]
    public async Task Confirm_ShouldHideWaitingSessionFromDifferentUser()
    {
        var interactive = new InteractiveRecordingHandler("interactive", canHandle: true);
        var sut = CreateOrchestrator(interactive);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", OwnerUserId);
        var waitingSession = await WaitUntilAsync(sut, sessionId, s => s.WaitingForConfirmation);

        sut.Confirm(sessionId, OtherUserId, accepted: true).Should().BeFalse();
        waitingSession.WaitingForConfirmation.Should().BeTrue();
        waitingSession.ConfirmationTcs!.Task.IsCompleted.Should().BeFalse();

        sut.Confirm(sessionId, OwnerUserId, accepted: true).Should().BeTrue();
        await WaitForResultAsync(sut, sessionId);
    }

    [Fact]
    public async Task StartImportAsync_ShouldStopAfterFailingMatchingPlugin()
    {
        var failing = new ThrowingHandler("failing", canHandle: true);
        var later = new RecordingHandler("later", canHandle: true);
        var sut = CreateOrchestrator(failing, later);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", OwnerUserId);

        var session = await WaitForResultAsync(sut, sessionId);
        session.Result!.Success.Should().BeFalse();
        session.Status.Should().StartWith("Failed:");
        later.CanHandleCalls.Should().Be(0);
    }

    [Fact]
    public async Task StartImportAsync_ShouldWaitForCollectionSelectionAndImportSelectedItems()
    {
        var collection = new CollectionRecordingHandler();
        var persister = new PassthroughPersister();
        var sut = CreateOrchestrator(persister, collection);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "collection.html", "https://example.test/collection", "cookbook-1", OwnerUserId);
        var waitingSession = await WaitUntilAsync(sut, sessionId, s => s.State == "SelectionRequired");

        waitingSession.CollectionPreview.Should().NotBeNull();
        waitingSession.CollectionPreview!.Items.Should().HaveCount(2);

        var selection = new ImportCollectionSelection([
            new ImportCollectionSelectionItem("item-2", "https://example.test/2", "cookbook-2")
        ]);

        sut.SubmitSelection(sessionId, OwnerUserId, selection).Success.Should().BeTrue();

        var completedSession = await WaitForResultAsync(sut, sessionId);
        completedSession.Result!.Success.Should().BeTrue();
        collection.ImportedItemIds.Should().Equal("item-2");
        persister.TargetCookbookIds.Should().Equal("cookbook-2");
        completedSession.CollectionItems.Should().ContainSingle(i => i.ItemId == "item-2" && i.State == ImportCollectionItemState.Succeeded);
    }

    [Fact]
    public async Task SubmitSelection_ShouldHideSelectionSessionFromDifferentUser()
    {
        var collection = new CollectionRecordingHandler();
        var sut = CreateOrchestrator(collection);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "collection.html", "https://example.test/collection", "cookbook-1", OwnerUserId);
        var waitingSession = await WaitUntilAsync(sut, sessionId, s => s.State == "SelectionRequired");

        var selection = new ImportCollectionSelection([
            new ImportCollectionSelectionItem("item-1", "https://example.test/1", "cookbook-1")
        ]);

        var result = sut.SubmitSelection(sessionId, OtherUserId, selection);

        result.IsNotFound.Should().BeTrue();
        waitingSession.State.Should().Be("SelectionRequired");
        waitingSession.SelectionTcs!.Task.IsCompleted.Should().BeFalse();

        sut.CancelSelection(sessionId, OwnerUserId).Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelSelection_ShouldHideSelectionSessionFromDifferentUser()
    {
        var collection = new CollectionRecordingHandler();
        var sut = CreateOrchestrator(collection);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "collection.html", "https://example.test/collection", "cookbook-1", OwnerUserId);
        var waitingSession = await WaitUntilAsync(sut, sessionId, s => s.State == "SelectionRequired");

        var result = sut.CancelSelection(sessionId, OtherUserId);

        result.IsNotFound.Should().BeTrue();
        waitingSession.State.Should().Be("SelectionRequired");
        waitingSession.SelectionTcs!.Task.IsCompleted.Should().BeFalse();
        waitingSession.Result.Should().BeNull();

        sut.CancelSelection(sessionId, OwnerUserId).Success.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitSelection_ShouldStillWorkAfterSelectionWasLeftOpen()
    {
        var collection = new CollectionRecordingHandler();
        var persister = new PassthroughPersister();
        var sut = CreateOrchestrator(persister, collection);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "collection.html", "https://example.test/collection", "cookbook-1", OwnerUserId);
        await WaitUntilAsync(sut, sessionId, s => s.State == "SelectionRequired");

        await Task.Delay(75);

        var selection = new ImportCollectionSelection([
            new ImportCollectionSelectionItem("item-1", "https://example.test/1", "cookbook-1")
        ]);

        sut.SubmitSelection(sessionId, OwnerUserId, selection).Success.Should().BeTrue();

        var completedSession = await WaitForResultAsync(sut, sessionId);
        completedSession.Result!.Success.Should().BeTrue();
        completedSession.State.Should().Be("Completed");
        collection.ImportedItemIds.Should().Equal("item-1");
        persister.TargetCookbookIds.Should().Equal("cookbook-1");
    }

    [Fact]
    public async Task StartImportAsync_ShouldFailCollectionPreviewWithDuplicateItemIds()
    {
        var collection = new CollectionRecordingHandler(duplicateIds: true);
        var sut = CreateOrchestrator(collection);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "collection.html", "https://example.test/collection", "cookbook-1", OwnerUserId);

        var failedSession = await WaitForResultAsync(sut, sessionId);
        failedSession.State.Should().Be("Failed");
        failedSession.Result!.Success.Should().BeFalse();
        failedSession.Result.Error.Should().Contain("doppelte Rezept-IDs");
        collection.ImportedItemIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitSelection_ShouldAcceptOnlyOneConcurrentSelection()
    {
        var collection = new CollectionRecordingHandler();
        var sut = CreateOrchestrator(collection);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "collection.html", "https://example.test/collection", "cookbook-1", OwnerUserId);
        await WaitUntilAsync(sut, sessionId, s => s.State == "SelectionRequired");

        var selection = new ImportCollectionSelection([
            new ImportCollectionSelectionItem("item-1", "https://example.test/1", "cookbook-1")
        ]);

        var submitTasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => sut.SubmitSelection(sessionId, OwnerUserId, selection)))
            .ToArray();

        var results = await Task.WhenAll(submitTasks);

        results.Count(r => r.Success).Should().Be(1);
        results.Count(r => !r.Success).Should().Be(19);

        var completedSession = await WaitForResultAsync(sut, sessionId);
        completedSession.Result!.Success.Should().BeTrue();
        collection.ImportedItemIds.Should().Equal("item-1");
    }

    [Fact]
    public async Task StartImportAsync_ShouldReportCancellationWhenTokenIsCancelledBeforeProcessingStarts()
    {
        var sut = CreateOrchestrator(new RecordingHandler("handler", canHandle: true));
        using var cts = new CancellationTokenSource();
        using var source = new CancellingStream([1], cts);

        var sessionId = await sut.StartImportAsync(source, "recipe.fixture", null, "cookbook-1", OwnerUserId, cts.Token);

        var session = await WaitForResultAsync(sut, sessionId);
        session.Result!.Success.Should().BeFalse();
        session.State.Should().Be("Failed");
        session.Status.Should().Be("Cancelled");
    }

    private static ImportOrchestrator CreateOrchestrator(params IImportHandler[] handlers)
    {
        return CreateOrchestrator(new PassthroughPersister(), handlers);
    }

    private static ImportOrchestrator CreateOrchestrator(IImportedRecipePersister persister, params IImportHandler[] handlers)
    {
        var services = new ServiceCollection()
            .AddSingleton(persister)
            .BuildServiceProvider();
        return new ImportOrchestrator(
            services.GetRequiredService<IServiceScopeFactory>(),
            new FakePluginManager(handlers),
            NullLogger<ImportOrchestrator>.Instance);
    }

    /// <summary>
    /// Stream that cancels the token once its content has been copied, so the background
    /// processing of the orchestrator starts with an already cancelled token.
    /// </summary>
    private sealed class CancellingStream(byte[] content, CancellationTokenSource cts) : MemoryStream(content)
    {
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await base.CopyToAsync(destination, bufferSize, cancellationToken);
            await cts.CancelAsync();
        }
    }

    private sealed class PassthroughPersister : IImportedRecipePersister
    {
        public List<string?> TargetCookbookIds { get; } = [];

        public Task<ImportResult> PersistAsync(ImportResult result, string? targetCookbookId, string userId, CancellationToken ct = default)
        {
            return Task.FromResult(result);
        }

        public Task<(bool Success, string? Error, string? RecipeId)> PersistRecipeAsync(ImportedRecipe imported, string? targetCookbookId, string userId, CancellationToken ct = default)
        {
            TargetCookbookIds.Add(targetCookbookId);
            return Task.FromResult<(bool Success, string? Error, string? RecipeId)>((true, null, $"{targetCookbookId}-recipe"));
        }
    }

    private static async Task<ImportOrchestrator.ImportSession> WaitForResultAsync(ImportOrchestrator sut, string sessionId)
    {
        return await WaitUntilAsync(sut, sessionId, s => s.Result is not null);
    }

    private static async Task<ImportOrchestrator.ImportSession> WaitUntilAsync(
        ImportOrchestrator sut,
        string sessionId,
        Func<ImportOrchestrator.ImportSession, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!timeout.IsCancellationRequested)
        {
            var session = sut.GetSessionForUser(sessionId, OwnerUserId);
            if (session is not null && predicate(session))
            {
                return session;
            }

            await Task.Delay(25, timeout.Token);
        }

        throw new TimeoutException($"Import session {sessionId} did not reach the expected state.");
    }

    private sealed class FakePluginManager(IReadOnlyList<IImportHandler> handlers) : IPluginManager
    {
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        {
            IReadOnlyList<PluginImportHandler> result = handlers
                .Select(handler => new PluginImportHandler(
                    new ImportPluginDescriptor(handler.GetType().Name, handler.GetType().Name, null, "1.0.0", "Tests", handler.GetType().FullName!, handler.GetType(), 0, PluginStatus.Loaded, null, null),
                    handler))
                .ToList();

            return Task.FromResult(result);
        }
    }

    private class RecordingHandler(string name, bool canHandle) : IImportHandler
    {
        // Exposed so derived handlers (e.g. InteractiveRecordingHandler) can read the same name
        // without capturing their own separate copy of it (see that class for why).
        protected string Name => name;

        public int CanHandleCalls { get; private set; }
        public int HandleCalls { get; private set; }
        public string UserId { private get; set; } = string.Empty;

        public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
        {
            CanHandleCalls++;
            return Task.FromResult(canHandle);
        }

        public virtual Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
        {
            HandleCalls++;
            return Task.FromResult(new ImportResult(true, null, [$"{name}-created"]));
        }
    }

    private sealed class InteractiveRecordingHandler : RecordingHandler, IInteractiveImportHandler
    {
        // Plain constructor (not a primary constructor): "name" only needs to reach the base
        // constructor here - a primary constructor would additionally capture its own separate
        // copy for this type's state (CS9107), when the inherited Name property (see
        // RecordingHandler) already exposes the same value below.
        public InteractiveRecordingHandler(string name, bool canHandle) : base(name, canHandle)
        {
        }

        public Task<ImportResult> HandleInteractiveAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, IImportInteraction interaction, CancellationToken ct = default)
        {
            return HandleAsync(interaction, ct);
        }

        private async Task<ImportResult> HandleAsync(IImportInteraction interaction, CancellationToken ct)
        {
            var accepted = await interaction.AskForConfirmationAsync("Import recipe?", ct);
            return accepted
                ? new ImportResult(true, null, [$"{Name}-confirmed"])
                : new ImportResult(false, "Import cancelled.", []);
        }
    }

    private sealed class ThrowingHandler(string name, bool canHandle) : RecordingHandler(name, canHandle)
    {
        public override Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
        {
            throw new InvalidOperationException("handler failed");
        }
    }

    private sealed class CollectionRecordingHandler : RecordingHandler, ICollectionImportHandler
    {
        private readonly bool _duplicateIds;

        public List<string> ImportedItemIds { get; } = [];

        public CollectionRecordingHandler(bool duplicateIds = false) : base("collection", canHandle: false)
        {
            _duplicateIds = duplicateIds;
        }

        public Task<ImportCollectionPreview?> TryReadCollectionPreviewAsync(Stream stream, string fileName, string? uri, CancellationToken ct = default)
        {
            ImportCollectionPreview? preview = new(
                "collection-1",
                "Collection",
                uri,
                [
                    new ImportCollectionItem("item-1", "One", "https://example.test/1"),
                    new ImportCollectionItem(_duplicateIds ? "item-1" : "item-2", "Two", "https://example.test/2")
                ]);
            return Task.FromResult<ImportCollectionPreview?>(preview);
        }

        public Task<ImportResult> ImportCollectionItemAsync(ImportCollectionItem item, string userId, CancellationToken ct = default)
        {
            ImportedItemIds.Add(item.Id);
            return Task.FromResult(new ImportResult(
                true,
                null,
                [],
                [new ImportedRecipe { Title = item.Title, SourceUri = item.Url }]));
        }
    }
}
