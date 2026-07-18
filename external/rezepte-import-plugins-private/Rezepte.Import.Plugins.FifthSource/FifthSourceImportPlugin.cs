using System.Text.Json;
using Rezepte.Import.Abstractions;
using Rezepte.Import.PluginSdk;

namespace Rezepte.Import.Plugins.FifthSource;

public sealed class FifthSourceImportPlugin : IImportPlugin
{
    public string Id => "fifth-source";
    public string DisplayName => "FifthSource";
    public string? Description => "Importiert Rezepte der fuenften URL-Quelle.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(FifthSourceImportHandler);
}

public sealed class FifthSourceImportHandler : UrlRecipeImportHandlerBase
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
            .FirstOrDefault(e => e.TryGetProperty("@type", out var type) &&
                (type.ValueKind == JsonValueKind.String && type.GetString() == "Recipe" ||
                 type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(t => t.GetString() == "Recipe")));

        if (recipeNode.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Kein Recipe-Knoten im Graph gefunden.");

        return new RecipeImport
        {
            Title = recipeNode.GetProperty("name").GetString(),
            Description = recipeNode.GetProperty("description").GetString(),
            Uri = recipeNode.GetProperty("mainEntityOfPage").GetString(),
            Portions = recipeNode.TryGetProperty("recipeYield", out var yield) && yield.ValueKind == JsonValueKind.Array
                ? yield[0].GetString().ToInt32Invariant(0)
                : yield.GetString().ToInt32Invariant(0),
            WorkTime = ParseIsoDurationToMinutes(recipeNode.GetProperty("prepTime").GetString()) + ParseIsoDurationToMinutes(recipeNode.GetProperty("cookTime").GetString()),
            Pictures = recipeNode.TryGetProperty("image", out var images) && images.ValueKind == JsonValueKind.Array
                ? images.EnumerateArray()
                    .Select(url => DownloadImageAsync(url.GetString()).Result)
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
                    .Select(step => new RecipeInstruction { Text = step.GetProperty("text").GetString() })
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
