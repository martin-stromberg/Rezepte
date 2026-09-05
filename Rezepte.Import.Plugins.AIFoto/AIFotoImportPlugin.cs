using Microsoft.Extensions.DependencyInjection;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;

namespace Rezepte.Import.Plugins.AIFoto;

/// <summary>
/// Plugin metadata and usability checks for AI photo based recipe import.
/// </summary>
public sealed class AIFotoImportPlugin : IImportPlugin
{
    private static readonly Type _handlerType = typeof(AIFotoImportHandler);

    /// <summary>
    /// Unique identifier of the plugin.
    /// </summary>
    public string Id => "ai-foto";

    /// <summary>
    /// Display name shown in the user interface.
    /// </summary>
    public string DisplayName => "AI-Foto";

    /// <summary>
    /// Description of the plugin.
    /// </summary>
    public string? Description => "Importiert Rezepte aus Fotos per KI.";

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
        var credentialsProvider = serviceProvider.GetRequiredService<IGoogleCredentialsProvider>();
        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient, ct);

        if (!credentialsProvider.GetDiagnostics().ServiceAccountFileExists)
        {
            issues.Add(new PluginUsabilityIssue(
                "Google Vision service account file is missing.",
                "Configure a valid Google service account file for Vision."));
        }

        if (!await settings.GetGlobalGoogleVisionEnabledAsync(ct))
        {
            issues.Add(new PluginUsabilityIssue(
                "Global Google Vision is disabled.",
                "Enable the global Google Vision switch in the AI settings."));
        }

        return PluginUsabilityResult.FromIssues(issues);
    }
}
