using Microsoft.Extensions.DependencyInjection;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;

namespace Rezepte.Import.Plugins.AIFoto;

public sealed class AIFotoImportPlugin : IImportPlugin
{
    public string Id => "ai-foto";
    public string DisplayName => "AI-Foto";
    public string? Description => "Importiert Rezepte aus Fotos per KI.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(AIFotoImportHandler);
    public int DefaultPriority => 1000;

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
