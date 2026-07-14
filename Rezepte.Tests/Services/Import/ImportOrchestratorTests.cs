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
    [Fact]
    public async Task StartImportAsync_ShouldUseActivePluginsInConfiguredOrder()
    {
        var first = new RecordingHandler("first", canHandle: false);
        var second = new RecordingHandler("second", canHandle: true);
        var sut = CreateOrchestrator(first, second);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1, 2, 3]), "recipe.fixture", null, "cookbook-1", "user-1");

        var session = await WaitForResultAsync(sut, sessionId);
        session.Result!.Success.Should().BeTrue();
        session.Result.CreatedRecipeIds.Should().Equal("second-created");
        first.CanHandleCalls.Should().Be(1);
        second.CanHandleCalls.Should().Be(1);
        second.HandleCalls.Should().Be(1);
    }

    [Fact]
    public async Task StartImportAsync_ShouldSupportInteractivePluginConfirmation()
    {
        var interactive = new InteractiveRecordingHandler("interactive", canHandle: true);
        var sut = CreateOrchestrator(interactive);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", "user-1");
        var waitingSession = await WaitUntilAsync(sut, sessionId, s => s.WaitingForConfirmation);

        waitingSession.ConfirmationPrompt.Should().Be("Import recipe?");
        sut.Confirm(sessionId, accepted: true).Should().BeTrue();

        var completedSession = await WaitForResultAsync(sut, sessionId);
        completedSession.Result!.Success.Should().BeTrue();
        completedSession.Result.CreatedRecipeIds.Should().Equal("interactive-confirmed");
    }

    [Fact]
    public async Task StartImportAsync_ShouldStopAfterFailingMatchingPlugin()
    {
        var failing = new ThrowingHandler("failing", canHandle: true);
        var later = new RecordingHandler("later", canHandle: true);
        var sut = CreateOrchestrator(failing, later);

        var sessionId = await sut.StartImportAsync(new MemoryStream([1]), "recipe.fixture", null, "cookbook-1", "user-1");

        var session = await WaitForResultAsync(sut, sessionId);
        session.Result!.Success.Should().BeFalse();
        session.Status.Should().StartWith("Failed:");
        later.CanHandleCalls.Should().Be(0);
    }

    private static ImportOrchestrator CreateOrchestrator(params IImportHandler[] handlers)
    {
        var services = new ServiceCollection()
            .AddSingleton<IImportedRecipePersister, PassthroughPersister>()
            .BuildServiceProvider();
        return new ImportOrchestrator(
            services.GetRequiredService<IServiceScopeFactory>(),
            new FakePluginManager(handlers),
            NullLogger<ImportOrchestrator>.Instance);
    }

    private sealed class PassthroughPersister : IImportedRecipePersister
    {
        public Task<ImportResult> PersistAsync(ImportResult result, string targetCookbookId, string userId, CancellationToken ct = default)
        {
            return Task.FromResult(result);
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
            var session = sut.GetSession(sessionId);
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
                    new ImportPluginDescriptor(handler.GetType().Name, handler.GetType().Name, null, "1.0.0", "Tests", handler.GetType().FullName!, handler.GetType(), 0, PluginStatus.Loaded, null),
                    handler))
                .ToList();

            return Task.FromResult(result);
        }
    }

    private class RecordingHandler(string name, bool canHandle) : IImportHandler
    {
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

    private sealed class InteractiveRecordingHandler(string name, bool canHandle) : RecordingHandler(name, canHandle), IInteractiveImportHandler
    {
        public Task<ImportResult> HandleInteractiveAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, IImportInteraction interaction, CancellationToken ct = default)
        {
            return HandleAsync(interaction, ct);
        }

        private async Task<ImportResult> HandleAsync(IImportInteraction interaction, CancellationToken ct)
        {
            var accepted = await interaction.AskForConfirmationAsync("Import recipe?", ct);
            return accepted
                ? new ImportResult(true, null, [$"{name}-confirmed"])
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
}
