using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Services.Import;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public class ImportServicePluginTests
{
    [Fact]
    public async Task ImportAsync_ShouldUseFirstMatchingPluginInConfiguredOrder()
    {
        var first = new RecordingHandler("first", canHandle: false);
        var second = new RecordingHandler("second", canHandle: true);
        var third = new RecordingHandler("third", canHandle: true);
        var sut = CreateService(first, second, third);

        var result = await sut.ImportAsync(new MemoryStream([1, 2, 3]), "recipe.html", "cookbook-1", "user-1");

        result.Success.Should().BeTrue();
        result.CreatedRecipeIds.Should().Equal("second-created");
        first.CanHandleCalls.Should().Be(1);
        second.HandleCalls.Should().Be(1);
        third.CanHandleCalls.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_ShouldReturnPluginError_WhenNoPluginCanHandleInput()
    {
        var sut = CreateService(new RecordingHandler("first", canHandle: false));

        var result = await sut.ImportAsync(new MemoryStream([1]), "recipe.bin", "cookbook-1", "user-1");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("No suitable import plugin found for this file or URL.");
    }

    private static ImportService CreateService(params RecordingHandler[] handlers)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var manager = new FakePluginManager(handlers);
        return new ImportService(manager, services, NullLogger<ImportService>.Instance);
    }

    private sealed class FakePluginManager(IReadOnlyList<RecordingHandler> handlers) : IPluginManager
    {
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        {
            IReadOnlyList<PluginImportHandler> result = handlers
                .Select(handler => new PluginImportHandler(
                    new ImportPluginDescriptor(handler.Name, handler.Name, null, "1.0.0", "Tests", handler.GetType().FullName!, handler.GetType(), PluginStatus.Loaded, null),
                    handler))
                .ToList();

            return Task.FromResult(result);
        }
    }

    private sealed class RecordingHandler(string name, bool canHandle) : IImportHandler
    {
        public string Name { get; } = name;
        public int CanHandleCalls { get; private set; }
        public int HandleCalls { get; private set; }
        public string UserId { private get; set; } = string.Empty;

        public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
        {
            CanHandleCalls++;
            return Task.FromResult(canHandle);
        }

        public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
        {
            HandleCalls++;
            return Task.FromResult(new ImportResult(true, null, [$"{Name}-created"]));
        }
    }
}
