using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Rezepte.Web.Entities;
using System.Formats.Asn1;
using static Rezepte.Web.Services.Import.GeminiClient;

namespace Rezepte.Web.Services.Import;

public class AIUrlImportHandler : BaseAIImportHandler, IImportHandler
{
    public AIUrlImportHandler(
        IOptionsMonitor<Configuration.AIOptions> options, 
        IRecipeService recipes, 
        ILogger<AIUrlImportHandler> logger, 
        IHttpContextAccessor httpContextAccessor, 
        IAiUsageService aiUsageService, 
        IGoogleServiceAccountProvider googleServiceAccountProvider,
        ISettingsService settings)
        :base(options, httpContextAccessor, aiUsageService, recipes, googleServiceAccountProvider, settings,logger)
    {
    }
   
    protected override bool IsTextMode()
    {
        return true;
    }

    protected override async Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
    {
        GeminiClient client = new GeminiClient(ServicecAcountFile);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url.Request", ct);
        var extractedRecipes = await client.ExtractRecipeFromUrlAsync(responseContent);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url.Success", ct);
        return extractedRecipes;
    }    
}
