using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.ThirdSource;

public sealed class ThirdSourceImportPlugin : IImportPlugin
{
    public string Id => "third-source";
    public string DisplayName => "ThirdSource";
    public string? Description => "Importiert Rezepte der dritten URL-Quelle.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(ThirdSourceImportHandler);
}

public sealed class ThirdSourceImportHandler : UrlRecipeImportHandlerBase
{
    protected override string FindTitle(string html)
    {
        var contentTitle = FindTagValue(html, "h1|class=recipe__header");
        return string.IsNullOrWhiteSpace(contentTitle) ? base.FindTitle(html) : contentTitle;
    }

    protected override string FindUrl(string html)
    {
        var url = base.FindUrl(html);
        if (string.IsNullOrWhiteSpace(url))
            url = FindTagValue(html, "link|rel=alternate");
        if (string.IsNullOrWhiteSpace(url))
            url = FindTagValue(html, "link|rel=canonical");
        return url;
    }

    protected override async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            var recipes = new List<RecipeImport>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var scriptContent in CollectScriptContents(responseContent))
            {
                try
                {
                    var elem = JsonSerializer.Deserialize<List<RootObject>>(scriptContent, options);
                    await foreach (var recipe in CropRecipesAsync(elem).ConfigureAwait(false))
                        recipes.Add(recipe);
                    continue;
                }
                catch
                {
                }

                try
                {
                    var elemSingle = JsonSerializer.Deserialize<RootObject>(scriptContent, options);
                    await foreach (var recipe in CropRecipesAsync([elemSingle]).ConfigureAwait(false))
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

    private async IAsyncEnumerable<RecipeImport> CropRecipesAsync(IEnumerable<RootObject?>? elem)
    {
        foreach (var e in (elem ?? []).Where(e => e?.Type == "Recipe").Cast<RootObject>())
        {
            var recipe = new RecipeImport
            {
                Title = e.Name,
                Description = e.Description,
                Ingredients = new RecipeIngredients
                {
                    Items = (e.RecipeIngredient?.ToArray() ?? [])
                        .Select(ing => ParseIngredientLine(ing))
                        .Select(i => new RecipeIngredient
                        {
                            Quantity = $"{i.Amount.ToString(CultureInfo.InvariantCulture)} {i.Unit}".Trim(),
                            Name = i.Name
                        })
                        .ToArray(),
                    Quantity = e.RecipeYield.ToInt32Invariant(0)
                },
                Instructions = new RecipeInstructions
                {
                    Steps = e.RecipeInstructions?.Select(text => new RecipeInstruction { Text = text.Text }).ToArray()
                },
                WorkTime = ParseIsoDurationToMinutes(e.PrepTime) + ParseIsoDurationToMinutes(e.CookTime),
                Portions = e.RecipeYield.ToInt32Invariant(0)
            };

            if (e.Image is not null && !string.IsNullOrWhiteSpace(e.Image.Url))
            {
                var image = await DownloadImageAsync(e.Image.Url.Trim('\'', '"')).ConfigureAwait(false);
                if (image is { Length: > 0 })
                    recipe.Pictures = [image];
            }

            if (IsComplete(recipe))
                yield return recipe;
        }
    }

    private static bool IsComplete(RecipeImport recipe)
    {
        return !string.IsNullOrWhiteSpace(recipe.Title)
            && recipe.Ingredients?.Items is { Length: > 0 }
            && recipe.Instructions?.Steps is { Length: > 0 };
    }

    public sealed class RootObject
    {
        [JsonPropertyName("@type")]
        public string? Type { get; set; }
        public string? Name { get; set; }
        [JsonConverter(typeof(ImageConverter))]
        public ImageWrapper? Image { get; set; }
        public string? Description { get; set; }
        public string? RecipeYield { get; set; }
        public string? PrepTime { get; set; }
        public string? CookTime { get; set; }
        public List<InstructionEntry>? RecipeInstructions { get; set; }
        public List<string>? RecipeIngredient { get; set; }
    }

    public sealed class InstructionEntry
    {
        public string? Text { get; set; }
    }

    public sealed class ImageWrapper
    {
        public string? Url { get; set; }
    }

    public sealed class ImageConverter : JsonConverter<ImageWrapper>
    {
        public override ImageWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
                return new ImageWrapper { Url = reader.GetString() };

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var jsonDoc = JsonDocument.ParseValue(ref reader);
                var root = jsonDoc.RootElement;
                return new ImageWrapper { Url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null };
            }

            throw new JsonException("Invalid format for ImageWrapper");
        }

        public override void Write(Utf8JsonWriter writer, ImageWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Url);
        }
    }
}
