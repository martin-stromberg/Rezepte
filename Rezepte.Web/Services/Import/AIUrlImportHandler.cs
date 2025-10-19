using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Rezepte.Web.Entities;
using System.Formats.Asn1;
using static Rezepte.Web.Services.Import.GeminiClient;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Handles the import of AI-generated recipes from URLs using the Gemini client.
/// </summary>
/// <remarks>This class extends <see cref="BaseAIImportHandler"/> and provides functionality for processing recipe
/// data extracted from URLs. It integrates with various services, including AI usage tracking, recipe management, and
/// Google credentials provisioning.</remarks>
public class AIUrlImportHandler : BaseAIImportHandler, IImportHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIUrlImportHandler"/> class.
    /// </summary>
    /// <param name="options">The configuration options for AI services, monitored for changes.</param>
    /// <param name="recipes">The service used to manage and retrieve recipes.</param>
    /// <param name="logger">The logger instance used to log diagnostic and operational messages.</param>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <param name="aiUsageService">The service used to track and manage AI usage metrics.</param>
    /// <param name="googleServiceAccountProvider">The provider for Google service account credentials.</param>
    /// <param name="settings">The service used to manage application settings.</param>
    public AIUrlImportHandler(
        IOptionsMonitor<Configuration.AIOptions> options, 
        IRecipeService recipes, 
        ILogger<AIUrlImportHandler> logger, 
        IAiUsageService aiUsageService, 
        IGoogleCredentialsProvider googleServiceAccountProvider,
        ISettingsService settings)
        :base(options, aiUsageService, recipes, googleServiceAccountProvider, settings,logger)
    {
    }

    protected override async Task<bool> IsActiveAsync()
    {
        if (string.IsNullOrEmpty(_serviceAccountProvider.GetGeminiApiKey()))
            if (!await base.IsActiveAsync())
                return false;
        if (!await SettingsService.GetGlobalAiEnabledAsync())
            return false;
        if (!await SettingsService.GetUserAiEnabledAsync(UserId))
            return false;
        var globalGeminiEnabled = await SettingsService.GetGlobalGeminiEnabledAsync();
        if (!globalGeminiEnabled)
            return false;
        var userGeminiEnabled = await SettingsService.GetUserGeminiEnabledAsync(UserId);
        if (!userGeminiEnabled)
            return false;
        return true;
    }

    /// <summary>
    /// Determines whether the current mode is text-based.
    /// </summary>
    /// <returns><see langword="true"/> if the current mode is text-based; otherwise, <see langword="false"/>.</returns>
    protected override bool IsTextMode()
    {
        return true;
    }
    protected override async Task<bool> HandleConfirmation(IImportInteraction interaction)
    {
        return await interaction.AskForConfirmationAsync("Der Inhalt der angegebenen Webseite wird mittels KI analysiert. Fortfahren?");
    }
    /// <summary>
    /// Reads a collection of AI-generated recipes from the specified input and returns them as an array.
    /// </summary>
    /// <remarks>This method uses an external client to extract recipes from the provided response content. It
    /// also records usage metrics for the request and its success status.</remarks>
    /// <param name="fileName">The name of the file associated with the recipe collection. This parameter is not used in the current
    /// implementation but may be reserved for future use.</param>
    /// <param name="stream">The input stream associated with the recipe collection. This parameter is not used in the current implementation
    /// but may be reserved for future use.</param>
    /// <param name="responseContent">The content of the response from which recipes will be extracted.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An array of <see cref="AIRecipe"/> objects representing the extracted recipes.</returns>
    protected override async Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
    {
        var client = CreateGeminiClient();
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url.Request", ct);
        var extractedRecipes = await client.ExtractRecipeFromUrlAsync(responseContent);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url.Success", ct);
        return extractedRecipes;
    }    
}
