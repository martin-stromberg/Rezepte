using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Rezepte.Import.Abstractions;
using static Rezepte.Web.Services.Import.GeminiClient;

namespace Rezepte.Import.Plugins.AIUrl;

public class AIUrlImportHandler(
    IOptionsMonitor<AIOptions> options,
    IRecipeService recipes,
    ILogger<AIUrlImportHandler> logger,
    IAiUsageService aiUsageService,
    IGeminiClient geminiClient,
    ISettingsService settings) : BaseAIImportHandler(options, aiUsageService, recipes, geminiClient, settings, logger), IImportHandler
{
    protected override async Task<bool> IsActiveAsync()
    {
        if (!CreateGeminiClient().HasApiKey() && !await base.IsActiveAsync()) return false;
        if (!await SettingsService.GetGlobalAiEnabledAsync() || !await SettingsService.GetUserAiEnabledAsync(UserId)) return false;
        if (!await SettingsService.GetGlobalGeminiEnabledAsync()) return false;
        return await SettingsService.GetUserGeminiEnabledAsync(UserId);
    }

    protected override bool IsTextMode() => true;

    protected override Task<bool> HandleConfirmation(IImportInteraction interaction) =>
        interaction.AskForConfirmationAsync("Der Inhalt der angegebenen Webseite wird mittels KI analysiert. Fortfahren?");

    protected override async Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
    {
        if (!await AiUsageService.TryRecordRequestAsync(UserId, "Gemini.Url", ct)) return [];
        var recipes = await CreateGeminiClient().ExtractRecipeFromUrlAsync(responseContent, ct);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url", AiRequestLogType.Success, ct);
        return recipes;
    }
}
