using System.Text.Json;
using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.FourthSource;

public sealed class FourthSourceImportPlugin : IImportPlugin
{
    public string Id => "fourth-source";
    public string DisplayName => "FourthSource";
    public string? Description => "Importiert Rezepte der vierten URL-Quelle.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(FourthSourceImportHandler);
}

public sealed class FourthSourceImportHandler : UrlRecipeImportHandlerBase
{
    protected override async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
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
                    var recipe = await ParseRecipeImportAsync(jsonDoc.RootElement).ConfigureAwait(false);
                    if (recipe is not null)
                        recipes.Add(recipe);
                }
                catch
                {
                }
            }

            return recipes.Count == 0
                ? new KeyValuePair<string, RecipeImport[]>()
                : new KeyValuePair<string, RecipeImport[]>(fileName, recipes.ToArray());
        }
        catch
        {
            return new KeyValuePair<string, RecipeImport[]>();
        }
    }

    private RecipeIngredient? ParseIngredient(JsonElement element)
    {
        var raw = element.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var ingredient = ParseIngredientLine(raw);
        return new RecipeIngredient { Quantity = $"{ingredient.Amount} {ingredient.Unit}".Trim(), Name = ingredient.Name };
    }

    private async Task<RecipeImport> ParseRecipeImportAsync(JsonElement root)
    {
        var pageProps = root.GetProperty("props").GetProperty("pageProps");
        var content = pageProps.GetProperty("content");
        var body = content.GetProperty("body");
        var structured = pageProps.GetProperty("recipeStructuredData");
        var imageUrl = structured.GetProperty("imageUrl").GetString();
        var imageData = await DownloadImageAsync(imageUrl).ConfigureAwait(false);

        return new RecipeImport
        {
            Title = structured.GetProperty("name").GetString(),
            Description = structured.GetProperty("description").GetString(),
            Uri = structured.GetProperty("canonicalUrl").GetString(),
            Portions = 1,
            WorkTime = ParseIsoDurationToMinutes(structured.GetProperty("prepTime").GetString()) + ParseIsoDurationToMinutes(structured.GetProperty("cookTime").GetString()),
            Pictures = imageData is null ? [] : [imageData],
            Ingredients = new RecipeIngredients
            {
                Items = structured.GetProperty("recipeIngredient").EnumerateArray()
                    .Select(ParseIngredient)
                    .Where(i => i is not null)
                    .ToArray(),
                Quantity = 1
            },
            Instructions = new RecipeInstructions
            {
                Steps = body.EnumerateArray()
                    .Where(e => e.TryGetProperty("component", out var c) && c.GetString() == "paragraph")
                    .Select(e => new RecipeInstruction { Text = e.GetProperty("text").GetString() })
                    .ToArray()
            }
        };
    }
}
