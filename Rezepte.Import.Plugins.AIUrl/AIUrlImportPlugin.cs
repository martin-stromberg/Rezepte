using Microsoft.Extensions.DependencyInjection;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;

namespace Rezepte.Import.Plugins.AIUrl;

public sealed class AIUrlImportPlugin : IImportPlugin
{
    public string Id => "ai-url";
    public string DisplayName => "AI-URL";
    public string? Description => "Importiert Rezepte aus Webseiten per KI.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(AIUrlImportHandler);
    public int DefaultPriority => 1000;

    public async Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var settings = serviceProvider.GetRequiredService<ISettingsService>();
        var geminiClient = serviceProvider.GetRequiredService<IGeminiClient>();
        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient, ct);

        return PluginUsabilityResult.FromIssues(issues);
    }
}
