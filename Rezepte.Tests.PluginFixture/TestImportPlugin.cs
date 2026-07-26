using Rezepte.Import.Abstractions;

namespace Rezepte.Tests.PluginFixture;

public sealed class TestImportPlugin : IImportPlugin
{
    public string Id => "external-test-plugin";
    public string DisplayName => "External Test Plugin";
    public string? Description => "PluginManager integration test fixture.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(TestImportHandler);
}

public sealed class InvalidHandlerPlugin : IImportPlugin
{
    public string Id => "invalid-handler-plugin";
    public string DisplayName => "Invalid Handler Plugin";
    public string? Description => "Plugin fixture with an invalid handler type.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(string);
}

public sealed class ThrowingUsabilityImportPlugin : IImportPlugin
{
    public string Id => "throwing-usability-plugin";
    public string DisplayName => "Throwing Usability Plugin";
    public string? Description => "Plugin fixture whose usability check always throws.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(TestImportHandler);

    public Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated usability check failure.");
}

public sealed class TestImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;

    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        return Task.FromResult(fileName.EndsWith(".fixture", StringComparison.OrdinalIgnoreCase));
    }

    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        return Task.FromResult(new ImportResult(true, null, ["external-fixture-recipe"]));
    }
}
