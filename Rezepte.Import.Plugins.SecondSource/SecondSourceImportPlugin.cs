using System.Text.Json;
using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.SecondSource;

public sealed class SecondSourceImportPlugin : IImportPlugin
{
    public string Id => "second-source";
    public string DisplayName => "SecondSource";
    public string? Description => "Importiert Rezepte der zweiten URL-Quelle.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(SecondSourceImportHandler);
}

public sealed class SecondSourceImportHandler : UrlRecipeImportHandlerBase
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
                    var recipe = ParseRecipeImportFromJsonElement(jsonDoc.RootElement);
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

    private RecipeIngredient? ParseIngredient(JsonElement element)
    {
        var raw = element.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var ingredient = ParseIngredientLine(raw);
        return new RecipeIngredient
        {
            Quantity = $"{ingredient.Amount} {ingredient.Unit}".Trim(),
            Name = ingredient.Name
        };
    }

    private RecipeImport ParseRecipeImportFromJsonElement(JsonElement recipeJson)
    {
        return new RecipeImport
        {
            Title = recipeJson.GetProperty("name").GetString(),
            Description = recipeJson.GetProperty("description").GetString(),
            Uri = recipeJson.GetProperty("mainEntityOfPage").GetProperty("@id").GetString(),
            Portions = recipeJson.GetProperty("recipeYield").GetString().ToInt32Invariant(0),
            WorkTime = ParseIsoDurationToMinutes(recipeJson.GetProperty("prepTime").GetString()) + ParseIsoDurationToMinutes(recipeJson.GetProperty("cookTime").GetString()),
            Pictures = recipeJson.TryGetProperty("image", out var images) && images.ValueKind == JsonValueKind.Array
                ? images.EnumerateArray()
                    .Select(i => i.TryGetProperty("url", out var url) ? DownloadImageAsync(url.GetString()).Result : null)
                    .Where(i => i is not null)
                    .Cast<byte[]>()
                    .ToArray()
                : [],
            Ingredients = new RecipeIngredients
            {
                Items = recipeJson.GetProperty("recipeIngredient").EnumerateArray()
                    .Select(ParseIngredient)
                    .Where(i => i is not null)
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
    }
}
