using Rezepte.Import.Abstractions;
using Rezepte.Web.Services;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Represents the gemini usability checks class.
/// </summary>
public static class GeminiUsabilityChecks
{
    /// <summary>
    /// collects the async.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="geminiClient">The gemini client parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public static async Task<List<PluginUsabilityIssue>> CollectAsync(
        ISettingsService settings,
        IGeminiClient geminiClient,
        CancellationToken ct = default)
    {
        var issues = new List<PluginUsabilityIssue>();

        if (!await settings.GetGlobalAiEnabledAsync(ct))
        {
            issues.Add(new PluginUsabilityIssue(
                "Global AI is disabled.",
                "Enable the global AI switch in the AI settings."));
        }

        if (!geminiClient.HasApiKey() && !geminiClient.HasServiceAccount())
        {
            issues.Add(new PluginUsabilityIssue(
                "Gemini authentication is missing.",
                "Configure a Gemini API key or a Google service account."));
        }

        if (!await settings.GetGlobalGeminiEnabledAsync(ct))
        {
            issues.Add(new PluginUsabilityIssue(
                "Global Gemini is disabled.",
                "Enable the global Gemini switch in the AI settings."));
        }

        return issues;
    }
}
