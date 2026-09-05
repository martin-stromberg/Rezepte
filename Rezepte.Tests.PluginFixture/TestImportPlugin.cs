using Rezepte.Import.Abstractions;

namespace Rezepte.Tests.PluginFixture;

/// <summary>
/// Test import plugin used as a plugin manager integration fixture.
/// </summary>
public sealed class TestImportPlugin : IImportPlugin
{
    private static readonly Type ImportHandlerType = typeof(TestImportHandler);

    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    public string Id => "external-test-plugin";

    /// <summary>
    /// Gets the plugin display name.
    /// </summary>
    public string DisplayName => "External Test Plugin";

    /// <summary>
    /// Gets the plugin description.
    /// </summary>
    public string? Description => "PluginManager integration test fixture.";

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Gets the import handler type provided by the plugin.
    /// </summary>
    public Type HandlerType => ImportHandlerType;
}

/// <summary>
/// Plugin fixture whose handler type is intentionally invalid.
/// </summary>
public sealed class InvalidHandlerPlugin : IImportPlugin
{
    private static readonly Type InvalidHandlerType = typeof(string);

    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    public string Id => "invalid-handler-plugin";

    /// <summary>
    /// Gets the plugin display name.
    /// </summary>
    public string DisplayName => "Invalid Handler Plugin";

    /// <summary>
    /// Gets the plugin description.
    /// </summary>
    public string? Description => "Plugin fixture with an invalid handler type.";

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Gets a handler type that does not implement <see cref="IImportHandler"/>.
    /// </summary>
    public Type HandlerType => InvalidHandlerType;
}

/// <summary>
/// Plugin fixture whose usability check always fails.
/// </summary>
public sealed class ThrowingUsabilityImportPlugin : IImportPlugin
{
    private static readonly Type ImportHandlerType = typeof(TestImportHandler);

    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    public string Id => "throwing-usability-plugin";

    /// <summary>
    /// Gets the plugin display name.
    /// </summary>
    public string DisplayName => "Throwing Usability Plugin";

    /// <summary>
    /// Gets the plugin description.
    /// </summary>
    public string? Description => "Plugin fixture whose usability check always throws.";

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Gets the import handler type provided by the plugin.
    /// </summary>
    public Type HandlerType => ImportHandlerType;

    /// <summary>
    /// Simulates a failing usability check.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The usability result; never returned because the check always throws.</returns>
    public Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        throw new InvalidOperationException("Simulated usability check failure.");
    }
}

/// <summary>
/// Import handler used by the plugin test fixtures.
/// </summary>
public sealed class TestImportHandler : IImportHandler
{
    /// <summary>
    /// Sets the owning user identifier.
    /// </summary>
    public string UserId { private get; set; } = string.Empty;

    /// <summary>
    /// Determines whether the handler accepts <c>.fixture</c> files.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="fileName">The source file name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><c>true</c> when the file name ends with <c>.fixture</c>.</returns>
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        return Task.FromResult(fileName.EndsWith(".fixture", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns a fixed successful import result for the fixture.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="fileName">The source file name.</param>
    /// <param name="uri">The optional source URI.</param>
    /// <param name="targetCookbookId">The target cookbook identifier.</param>
    /// <param name="userId">The importing user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The simulated import result.</returns>
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        return Task.FromResult(new ImportResult(true, null, ["external-fixture-recipe"]));
    }
}
