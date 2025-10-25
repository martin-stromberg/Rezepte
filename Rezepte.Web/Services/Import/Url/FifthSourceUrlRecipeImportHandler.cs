

using Rezepte.Web.Extensions;
using System.Text.Json;

namespace Rezepte.Web.Services.Import.Url;

/// <summary>
/// Unterstützung für
/// - k o c h k a r u s s e l l.com
/// </summary>
public class FifthSourceUrlRecipeImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public FifthSourceUrlRecipeImportHandler(IRecipeService recipes, ILogger<FifthSourceUrlRecipeImportHandler> logger) : base(recipes, logger)
    {
    }

    protected override Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
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
                    var recipe = ParseRecipeImportFromGraph(root);
                    if (recipe is not null)
                        recipes.Add(recipe);
                }
                catch { }
            if (!recipes.Any())
                throw new ApplicationException("no recipes");
            return Task.FromResult(new KeyValuePair<string, RecipeImport[]>(fileName, recipes.ToArray()));
        }
        catch
        {
            return Task.FromResult(new KeyValuePair<string, RecipeImport[]>());
        }
    }
    private RecipeImport ParseRecipeImportFromGraph(JsonElement graphRoot)
    {
        var recipeNode = graphRoot.GetProperty("@graph").EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("@type", out var type) &&
                (type.ValueKind == JsonValueKind.String && type.GetString() == "Recipe" ||
                 type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(t => t.GetString() == "Recipe")));

        if (recipeNode.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Kein Recipe-Knoten im Graph gefunden.");

        var import = new RecipeImport
        {
            Title = recipeNode.GetProperty("name").GetString(),
            Description = recipeNode.GetProperty("description").GetString(),
            Uri = recipeNode.GetProperty("mainEntityOfPage").GetString(),
            Portions = recipeNode.TryGetProperty("recipeYield", out var yield) && yield.ValueKind == JsonValueKind.Array
                ? yield[0].GetString().ToInt32(0)
                : yield.GetString().ToInt32(0),
            WorkTime = ParseIsoDurationToMinutes(recipeNode.GetProperty("prepTime").GetString()) +
                       ParseIsoDurationToMinutes(recipeNode.GetProperty("cookTime").GetString()),
            Pictures = recipeNode.TryGetProperty("image", out var images) && images.ValueKind == JsonValueKind.Array
                ? images.EnumerateArray().Select(url => DownloadImageAsync(url.GetString()).Result).ToArray()
                : Array.Empty<byte[]>(),
            Ingredients = new RecipeIngredients
            {
                Items = recipeNode.GetProperty("recipeIngredient").EnumerateArray()
                    .Select(ParseIngredient)
                    .Where(i => i != null)
                    .ToArray(),
                Quantity = 1
            },
            Instructions = new RecipeInstructions
            {
                Steps = recipeNode.GetProperty("recipeInstructions").EnumerateArray()
                    .Select(step => new RecipeInstruction { Text = step.GetProperty("text").GetString() })
                    .ToArray()
            }
        };

        return import;
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

}
