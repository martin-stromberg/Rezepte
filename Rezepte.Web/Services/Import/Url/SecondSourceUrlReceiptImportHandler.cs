using Rezepte.Web.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Xml;

namespace Rezepte.Web.Services.Import.Url;

/// <summary>
/// Unterstützung für
/// - l e c k e r.de
/// </summary>
public class SecondSourceUrlReceiptImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public SecondSourceUrlReceiptImportHandler(IRecipeService recipes, ILogger<SecondSourceUrlReceiptImportHandler> logger) : base (recipes, logger)
    {
    }

    protected override async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            List<RecipeImport> recipes = new List<RecipeImport>();
            foreach (var scriptContent in CollectScriptContents(responseContent))
                try
                {
                    using var jsonDoc = JsonDocument.Parse(scriptContent, new JsonDocumentOptions()
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                        MaxDepth = 20
                    });
                    var root = jsonDoc.RootElement;
                    var recipe = ParseRecipeImportFromJsonElement(root);
                    if (recipe is not null)
                        recipes.Add(recipe);
                }
                catch
                {

                }
            if (!recipes.Any())
                throw new ApplicationException("no recipes");
            return new KeyValuePair<string, RecipeImport[]>(fileName, recipes.ToArray());
        }
        catch
        {
            return new KeyValuePair<string, RecipeImport[]>();
        }
    }
    private RecipeIngredient? ParseIngredient(JsonElement element)
    {
        var raw = element.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var ingr = base.ParseIngredientLine(raw);
        if (ingr is not null)
            return new RecipeIngredient()
            {
                Quantity = $"{ingr.Amount} {ingr.Unit}".Trim(),
                Name = ingr.Name
            };

        var parts = raw.Split(' ', 2);
        return parts.Length == 2
            ? new RecipeIngredient { Quantity = parts[0], Name = parts[1] }
            : new RecipeIngredient { Quantity = "", Name = raw };
    }
    private RecipeImport ParseRecipeImportFromJsonElement(JsonElement recipeJson)
    {        
        var import = new RecipeImport
        {
            Title = recipeJson.GetProperty("name").GetString(),
            Description = recipeJson.GetProperty("description").GetString(),
            Uri = recipeJson.GetProperty("mainEntityOfPage").GetProperty("@id").GetString(),
            Portions = recipeJson.GetProperty("recipeYield").GetString().ToInt32(0),
            WorkTime = ParseIsoDurationToMinutes(recipeJson.GetProperty("prepTime").GetString()) + ParseIsoDurationToMinutes(recipeJson.GetProperty("cookTime").GetString()),
            Pictures = recipeJson.TryGetProperty("image", out var images) && images.ValueKind == JsonValueKind.Array
                ? images.EnumerateArray().Select(i =>
                {
                    var imageUrl = i.GetProperty("url").GetString();
                    var imageData = base.DownloadImageAsync(imageUrl).Result;
                    return imageData;
                }).ToArray()
                : Array.Empty<byte[]>(),
            Ingredients = new RecipeIngredients
            {
                Items = recipeJson.GetProperty("recipeIngredient").EnumerateArray()
                    .Select(ParseIngredient)
                    .Where(i => i != null)
                    .ToArray(),
                Quantity = 1
            },
            Instructions = new RecipeInstructions
            {
                Steps = recipeJson.GetProperty("recipeInstructions").EnumerateArray()
                    .Select(i => new RecipeInstruction { Text = i.GetProperty("text").GetString() })
                    .ToArray()
            }
        };

        return import;
    }
    
}
