using System.Text.Json;
using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.SixthSource;

public sealed class SixthSourceImportPlugin : IImportPlugin
{
    public string Id => "sixth-source";
    public string DisplayName => "SixthSource";
    public string? Description => "Importiert Rezepte der sechsten URL-Quelle.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(SixthSourceImportHandler);
}

public sealed class SixthSourceImportHandler : UrlRecipeImportHandlerBase
{
    protected override Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            var recipes = new List<RecipeImport>();
            foreach (var scriptContent in CollectScriptContents(responseContent))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(scriptContent, new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                        MaxDepth = 20
                    });
                    var recipe = ParseRecipeImportFromGraph(jsonDoc.RootElement);
                    if (recipe is not null)
                        recipes.Add(recipe);
                }
                catch
                {
                }
            }

            return Task.FromResult(recipes.Count == 0
                ? new KeyValuePair<string, RecipeImport[]>()
                : new KeyValuePair<string, RecipeImport[]>(fileName, recipes.ToArray()));
        }
        catch
        {
            return Task.FromResult(new KeyValuePair<string, RecipeImport[]>());
        }
    }

    private RecipeImport ParseRecipeImportFromGraph(JsonElement graphRoot)
    {
        var recipeNode = graphRoot.GetProperty("@graph").EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("@type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "Recipe");

        if (recipeNode.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Kein Recipe-Knoten im Graph gefunden.");

        return new RecipeImport
        {
            Title = recipeNode.GetProperty("name").GetString(),
            Description = recipeNode.GetProperty("description").GetString(),
            Uri = recipeNode.TryGetProperty("author", out var author) && author.TryGetProperty("url", out var urlProp)
                ? urlProp.GetString()
                : "https://www.daskochrezept.de",
            Portions = recipeNode.TryGetProperty("recipeYield", out var yield) && yield.ValueKind == JsonValueKind.Array
                ? yield[0].GetString().ToInt32Invariant(0)
                : yield.GetString().ToInt32Invariant(0),
            WorkTime = ParseIsoDurationToMinutes(recipeNode.GetProperty("prepTime").GetString()) + ParseIsoDurationToMinutes(recipeNode.GetProperty("cookTime").GetString()),
            Pictures = recipeNode.TryGetProperty("image", out var imageObj) &&
                       imageObj.TryGetProperty("url", out var urls) &&
                       urls.ValueKind == JsonValueKind.Array
                ? urls.EnumerateArray()
                    .Select(u => DownloadImageAsync(u.GetString()).Result)
                    .Where(i => i is not null)
                    .Cast<byte[]>()
                    .ToArray()
                : [],
            Ingredients = new RecipeIngredients
            {
                Items = recipeNode.GetProperty("recipeIngredient").EnumerateArray()
                    .Select(ParseIngredient)
                    .Where(i => i is not null)
                    .ToArray(),
                Quantity = 1
            },
            Instructions = new RecipeInstructions
            {
                Steps = recipeNode.GetProperty("recipeInstructions").EnumerateArray()
                    .Select(text => new RecipeInstruction { Text = text.GetString() })
                    .ToArray()
            }
        };
    }

    private RecipeIngredient? ParseIngredient(JsonElement element)
    {
        var raw = element.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var ingredient = ParseIngredientLine(raw);
        return new RecipeIngredient { Quantity = $"{ingredient.Amount} {ingredient.Unit}".Trim(), Name = ingredient.Name };
    }
}
