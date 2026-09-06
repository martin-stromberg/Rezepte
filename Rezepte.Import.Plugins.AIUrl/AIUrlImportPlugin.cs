using Microsoft.Extensions.DependencyInjection;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;

namespace Rezepte.Import.Plugins.AIUrl;

/// <summary>
/// Plugin metadata and usability checks for AI URL based recipe import.
/// </summary>
public sealed class AIUrlImportPlugin : IImportPlugin
{
    private static readonly Type _handlerType = typeof(AIUrlImportHandler);

    /// <summary>
    /// Unique identifier of the plugin.
    /// </summary>
    public string Id => "ai-url";

    /// <summary>
    /// Display name shown in the user interface.
    /// </summary>
    public string DisplayName => "AI-URL";

    /// <summary>
    /// Description of the plugin.
    /// </summary>
    public string? Description => "Importiert Rezepte aus Webseiten per KI.";

    /// <summary>
    /// Version of the plugin.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Type of the handler that performs the import.
    /// </summary>
    public Type HandlerType => _handlerType;

    /// <summary>
    /// Default priority used when multiple plugins can handle the same input.
    /// </summary>
    public int DefaultPriority => 1000;

    /// <summary>
    /// Checks whether the plugin can be used in the current environment.
    /// </summary>
    /// <param name="serviceProvider">Application service provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating usability and any issues.</returns>
    public async Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var settings = serviceProvider.GetRequiredService<ISettingsService>();
        var geminiClient = serviceProvider.GetRequiredService<IGeminiClient>();
        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient, ct);

        return PluginUsabilityResult.FromIssues(issues);
    }
}
