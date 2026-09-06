using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.AIUrl;

/// <summary>
/// Import handler that extracts recipes from web pages using Gemini.
/// </summary>
public class AIUrlImportHandler : BaseAIImportHandler, IImportHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIUrlImportHandler"/> class.
    /// </summary>
    /// <param name="options">AI options monitor.</param>
    /// <param name="recipes">Recipe service used to persist imported recipes.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="aiUsageService">AI usage accounting service.</param>
    /// <param name="geminiClient">Gemini client for recipe extraction.</param>
    /// <param name="settings">Settings service.</param>
    public AIUrlImportHandler(
        IOptionsMonitor<AIOptions> options,
        IRecipeService recipes,
        ILogger<AIUrlImportHandler> logger,
        IAiUsageService aiUsageService,
        IGeminiClient geminiClient,
        ISettingsService settings)
        : base(options, aiUsageService, recipes, geminiClient, settings, logger)
    {
    }

    /// <summary>
    /// Determines whether the handler is active for the current user and configuration.
    /// </summary>
    /// <returns><c>true</c> when the handler can be used; otherwise <c>false</c>.</returns>
    protected override async Task<bool> IsActiveAsync()
    {
        if (!await base.IsActiveAsync()) return false;
        if (!HasGeminiAuthentication()) return false;
        if (!await SettingsService.GetGlobalGeminiEnabledAsync())
        {
            LogInactive("global Gemini is disabled");
            return false;
        }
        if (!await SettingsService.GetUserGeminiEnabledAsync(UserId))
        {
            LogInactive("user Gemini is disabled");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the handler works with text input.
    /// </summary>
    /// <returns><c>true</c> because this handler processes URL or HTML text.</returns>
    protected override bool IsTextMode() => true;

    /// <summary>
    /// Asks the user to confirm the AI analysis of the provided web page.
    /// </summary>
    /// <param name="interaction">Interaction surface used to ask for confirmation.</param>
    /// <returns><c>true</c> when the user confirmed; otherwise <c>false</c>.</returns>
    protected override Task<bool> HandleConfirmation(IImportInteraction interaction) =>
        interaction.AskForConfirmationAsync("Der Inhalt der angegebenen Webseite wird mittels KI analysiert. Fortfahren?");

    /// <summary>
    /// Reads recipes from the provided URL response using Gemini.
    /// </summary>
    /// <param name="fileName">Name of the uploaded file or URL.</param>
    /// <param name="stream">Stream containing the response data.</param>
    /// <param name="responseContent">Text content of the response.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An array of extracted recipes.</returns>
    protected override async Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
    {
        if (!await AiUsageService.TryRecordRequestAsync(UserId, "Gemini.Url", ct)) return [];
        var recipes = await CreateGeminiClient().ExtractRecipeFromUrlAsync(responseContent, ct);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url", AiRequestLogType.Success, ct);
        return recipes;
    }
}
