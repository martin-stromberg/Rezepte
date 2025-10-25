using Rezepte.Web.Components.Shared;
using Rezepte.Web.Entities;
using Rezepte.Web.Extensions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Rezepte.Web.Services.Import.Url;
/// <summary>
/// Unterstützung für 
/// - L I D L.de Rezeptseiten
/// - A l d i S u e d.de
/// </summary>
public class ThirdSourceUrlReceiptImportHandler: BaseUrlReceiptImportHandler, IImportHandler
{
    public ThirdSourceUrlReceiptImportHandler(IRecipeService recipes, ILogger<ThirdSourceUrlReceiptImportHandler> logger) : base (recipes, logger)
    {
    }
    protected override string FindTitle(string html)
    {
        var contentTitle = FindTagValue(html, "h1|class=recipe__header");
        if (!string.IsNullOrWhiteSpace(contentTitle))
            return contentTitle;
        return base.FindTitle(html);
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
            List<RecipeImport> recipes = new List<RecipeImport>();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            foreach (var scriptContent in CollectScriptContents(responseContent))
            {
                try
                {
                    var elem = System.Text.Json.JsonSerializer.Deserialize<List<RootObject>>(scriptContent, options);
                    await foreach (var r in CropRecipesAsync(elem))
                    {
                        recipes.Add(r);
                    }
                    continue;
                }
                catch { }
                try
                {
                    var elemSingle = System.Text.Json.JsonSerializer.Deserialize<RootObject>(scriptContent, options);
                    await foreach (var r in CropRecipesAsync(new List<RootObject>() { elemSingle }))
                    {
                        recipes.Add(r);
                    }
                }
                catch { }
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

    private async IAsyncEnumerable<RecipeImport> CropRecipesAsync(List<RootObject>? elem)
    {
        foreach (var e in (elem ?? Enumerable.Empty<RootObject>()).Where(e => e.Type == "Recipe"))
        {
            var recipe = new RecipeImport()
            {
                Title = e.Name,
                Description = e.Description,
                Ingredients = new RecipeIngredients()
                {
                    Items = (e.RecipeIngredient?.ToArray() ?? Array.Empty<string>())
                        .Select(ing => ParseIngredient(ing))
                        .Where(i => i is not null)
                        .Select(i =>
                        {
                                return new RecipeIngredient
                                {
                                    Quantity =$"{i.Amount.ToString(CultureInfo.InvariantCulture)} {i.Unit}".Trim(),
                                    Name = i.Name ?? string.Empty,
                                    
                                };
                        })
                        .Where(i => i is not null)
                        .ToArray(),
                    Quantity = e.RecipeYield.ToInt32(0)
                },
                Instructions = new RecipeInstructions(){
                    Steps = e.RecipeInstructions?.Select(text => new RecipeInstruction()
                    {
                        Text = text.Text
                    }).ToArray()
                },
                WorkTime = ParseIsoDurationToMinutes(e.PrepTime) + ParseIsoDurationToMinutes(e.CookTime),
                Portions = e.RecipeYield.ToInt32(0)
            };

            if (e.Image is not null && !string.IsNullOrWhiteSpace(e.Image.Url))
            {
                var pictureTempPath = await DownloadFileAsync(e.Image.Url.Trim('\'', '"'));
                try
                {
                    byte[] imageArray = File.ReadAllBytes(pictureTempPath);
                    recipe.Pictures = new byte[][] { imageArray };
                }
                finally
                {
                    File.Delete(pictureTempPath);
                }
            }
            
            if (IsComplete(recipe))
                yield return recipe;
        }
    }

    private bool IsComplete(RecipeImport recipe)
    {
        return !string.IsNullOrWhiteSpace(recipe.Title)
            && recipe.Ingredients is not null
            && recipe.Ingredients.Items is not null
            && recipe.Ingredients.Items.Any()
            && recipe.Instructions is not null
            && recipe.Instructions.Steps is not null
            && recipe.Instructions.Steps.Any();
    }

    public class RootObject
    {
        [JsonPropertyName("@context")]
        public string Context { get; set; }

        [JsonPropertyName("@type")]
        public string Type { get; set; }

        [JsonPropertyName("@id")]
        public string Id { get; set; }

        public List<ItemListElement> ItemListElement { get; set; } // für BreadcrumbList

        // Für Recipe
        public string Name { get; set; }
        public string ThumbnailUrl { get; set; }
        public Author Author { get; set; }
        public MainEntityOfPage MainEntityOfPage { get; set; }

        [JsonConverter(typeof(ImageConverter))]
        public ImageWrapper Image { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string RecipeYield { get; set; }
        public string PrepTime { get; set; }
        public string CookTime { get; set; }
        public Nutrition Nutrition { get; set; }
        public AggregateRating AggregateRating { get; set; }
        public List<InstructionEntry> RecipeInstructions { get; set; }
        public List<Tool> Tool { get; set; }
        public List<string> RecipeIngredient { get; set; }
        public string RecipeCategory { get; set; }
    }

    public class ItemListElement
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }
        public int Position { get; set; }
        public Item Item { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Author
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public class MainEntityOfPage
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }

        [JsonPropertyName("@id")]
        public string Id { get; set; }

        public Breadcrumb Breadcrumb { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class Breadcrumb
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; }
    }

    public class Nutrition
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }
        public string Calories { get; set; }
    }

    public class AggregateRating
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }
        public string RatingValue { get; set; }
        public string RatingCount { get; set; }
    }

    public class InstructionEntry
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }
        public string Text { get; set; }
        public string Image { get; set; }
    }

    public class Tool
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }
        public string Item { get; set; }
        public int RequiredQuantity { get; set; }
    }

    public class ImageWrapper
    {
        public string Url { get; set; }
    }
    public class ImageConverter : JsonConverter<ImageWrapper>
    {
        public override ImageWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string url = reader.GetString();
                return new ImageWrapper { Url = url };
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var jsonDoc = JsonDocument.ParseValue(ref reader);
                var root = jsonDoc.RootElement;
                string url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                return new ImageWrapper { Url = url };
            }

            throw new JsonException("Invalid format for ImageWrapper");
        }

        public override void Write(Utf8JsonWriter writer, ImageWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Url);
        }
    }

}
