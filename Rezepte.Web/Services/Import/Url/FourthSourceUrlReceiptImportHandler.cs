using Rezepte.Web.Extensions;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Rezepte.Web.Services.Import.Url.ThirdSourceUrlReceiptImportHandler;

namespace Rezepte.Web.Services.Import.Url;

/// <summary>
/// Unterstützung für
/// - d a s k o c h r e z e p t.de
/// </summary>
public class SixthSourceUrlRecipeImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public SixthSourceUrlRecipeImportHandler(IRecipeService recipes, ILogger<SixthSourceUrlRecipeImportHandler> logger) : base(recipes, logger)
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
                    var recipe = ParseRecipeImportFromGraph(root);
                    if (recipe is not null)
                        recipes.Add(recipe);
                }
                catch { }
            if (!recipes.Any())
                throw new ApplicationException("no recipes");
            return new KeyValuePair<string, RecipeImport[]>(fileName, recipes.ToArray());
        }
        catch
        {
            return new KeyValuePair<string, RecipeImport[]>();
        }
    }
    private RecipeImport ParseRecipeImportFromGraph(JsonElement graphRoot)
    {
        var recipeNode = graphRoot.GetProperty("@graph").EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("@type", out var type) &&
                (type.ValueKind == JsonValueKind.String && type.GetString() == "Recipe"));

        if (recipeNode.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Kein Recipe-Knoten im Graph gefunden.");

        var import = new RecipeImport
        {
            Title = recipeNode.GetProperty("name").GetString(),
            Description = recipeNode.GetProperty("description").GetString(),
            Uri = recipeNode.TryGetProperty("author", out var author) && author.TryGetProperty("url", out var urlProp)
                ? urlProp.GetString()
                : "https://www.daskochrezept.de",
            Portions = recipeNode.TryGetProperty("recipeYield", out var yield) && yield.ValueKind == JsonValueKind.Array
                ? yield[0].GetString().ToInt32(0)
                : yield.GetString().ToInt32(0),
            WorkTime = ParseIsoDurationToMinutes(recipeNode.GetProperty("prepTime").GetString()) +
                       ParseIsoDurationToMinutes(recipeNode.GetProperty("cookTime").GetString()),
            Pictures = recipeNode.TryGetProperty("image", out var imageObj) &&
                       imageObj.TryGetProperty("url", out var urls) &&
                       urls.ValueKind == JsonValueKind.Array
                ? urls.EnumerateArray().Select(u => DownloadImageAsync(u.GetString()).Result).ToArray()
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
                    .Select(text => new RecipeInstruction { Text = text.GetString() })
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
/// <summary>
/// Unterstützung für 
/// - K a b e l e i n s.de
/// </summary>
public class FourthSourceUrlReceiptImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public FourthSourceUrlReceiptImportHandler(IRecipeService recipes, ILogger<FourthSourceUrlReceiptImportHandler> logger) : base(recipes, logger)
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
                    var recipe = await ParseRecipeImportAsync(root);
                    if (recipe is not null)
                        recipes.Add(recipe);
                }
                catch { }
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
    private async Task<RecipeImport> ParseRecipeImportAsync(JsonElement root)
    {
        var pageProps = root.GetProperty("props").GetProperty("pageProps");
        var seo = pageProps.GetProperty("seoMetaData");
        var content = pageProps.GetProperty("content");
        var body = content.GetProperty("body");
        var structured = pageProps.GetProperty("recipeStructuredData");
        var imageUrl = structured.GetProperty("imageUrl").GetString();
        var imageData = await DownloadImageAsync(imageUrl);
        var import = new RecipeImport
        {
            Title = structured.GetProperty("name").GetString(),
            Description = structured.GetProperty("description").GetString(),
            Uri = structured.GetProperty("canonicalUrl").GetString(),
            Portions = 1, // nicht im JSON enthalten, ggf. aus "recipeYield" ergänzen
            WorkTime = ParseIsoDurationToMinutes(structured.GetProperty("prepTime").GetString()) + ParseIsoDurationToMinutes(structured.GetProperty("cookTime").GetString()),
            Pictures = (imageData is null)? new byte[0][] : new[] { imageData },
            Ingredients = new RecipeIngredients
            {
                Items = structured.GetProperty("recipeIngredient").EnumerateArray()
                    .Select(ParseIngredient)
                    .Where(i => i != null)
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

        return import;
    }
}
