using Rezepte.Import.Abstractions;
using System.Net;
using System.Xml;

namespace Rezepte.Import.Plugins.Chefkoch;

public sealed class ChefkochImportPlugin : IImportPlugin
{
    public string Id => "chefkoch";
    public string DisplayName => "Chefkoch";
    public string? Description => "Importiert Rezepte von Chefkoch.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(ChefkochImportHandler);
}

public sealed class ChefkochImportHandler : UrlRecipeImportHandlerBase
{
    protected override string FindTitle(string html)
    {
        html = FindTagValue(html, "body", "main");
        var contentTitle = FindTagValue(html, "h1");
        return string.IsNullOrWhiteSpace(contentTitle) ? base.FindTitle(html) : contentTitle;
    }

    protected override async Task<byte[][]?> FindPicturesAsync(string html)
    {
        var tags = CollectTags(html, "div|class=ds-slider__item")
            .Where(t => t.Contains("ds-slider-image__image-wrap", StringComparison.Ordinal))
            .SelectMany(t => CollectTagValues(t, "img"))
            .ToArray();

        var imageDataCollection = new List<byte[]>();
        foreach (var imageUri in tags)
        {
            try
            {
                var image = await DownloadImageAsync(imageUri).ConfigureAwait(false);
                if (image is { Length: > 0 })
                    imageDataCollection.Add(image);
            }
            catch
            {
            }
        }

        return imageDataCollection.Concat(await base.FindPicturesAsync(html).ConfigureAwait(false) ?? []).ToArray();
    }

    protected override async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            var recipe = new RecipeImport
            {
                Title = WebUtility.HtmlDecode(FindTitle(responseContent)),
                Uri = FindUrl(responseContent),
                Pictures = await FindPicturesAsync(responseContent).ConfigureAwait(false)
            };

            responseContent = FindTagValue(responseContent, "body", "main");
            var articles = CollectTags(responseContent, "section");
            if (articles.Length == 0)
                throw new ApplicationException("no sections");

            recipe.Ingredients = FindIngredients(articles) ?? throw new ApplicationException("no ingredients");
            recipe.Instructions = FindInstructions(articles) ?? throw new ApplicationException("no instructions");
            recipe.WorkTime = (int)ParseGermanTimeSpan(FindMetaValue(responseContent, "Arbeitszeit")).TotalMinutes;

            if (recipe.Instructions.Steps is null || recipe.Instructions.Steps.Length == 0)
                throw new ApplicationException("no instructions");

            return new KeyValuePair<string, RecipeImport[]>(fileName, [recipe]);
        }
        catch
        {
            return new KeyValuePair<string, RecipeImport[]>();
        }
    }

    private RecipeIngredients? FindIngredients(string[] articles)
    {
        var ingredientsArticle = articles.FirstOrDefault(article => article.Contains("recipe-ingredients", StringComparison.Ordinal));
        if (ingredientsArticle is null)
            return null;

        var ingredientTable = FindTag(ingredientsArticle, false, "table");
        var rows = CollectTags(ingredientTable, "tr");
        return new RecipeIngredients
        {
            Quantity = FindTagValue(ingredientsArticle, "input").ToInt32Invariant(0),
            Items = rows.Select(row =>
                {
                    var node = CreateNode(row);
                    var quantityCell = node?.ChildNodes.OfType<XmlNode>().FirstOrDefault(n => n.Name == "td");
                    var nameCell = node?.ChildNodes.OfType<XmlNode>().Skip(1).FirstOrDefault(n => n.Name == "td");
                    return quantityCell is null || nameCell is null
                        ? null
                        : new RecipeIngredient { Quantity = quantityCell.InnerText, Name = GetNodeText(nameCell) };
                })
                .Where(i => i is not null)
                .ToArray()
        };
    }

    private RecipeInstructions? FindInstructions(string[] articles)
    {
        var instructionArticle = articles.FirstOrDefault(article => article.Contains("Zubereitung", StringComparison.Ordinal));
        if (instructionArticle is null)
            return null;

        var instructionRows = CollectTags(instructionArticle, "div")
            .Where(r => r.Contains("instruction-row", StringComparison.Ordinal))
            .ToList();

        return new RecipeInstructions
        {
            Steps = instructionRows.Select(r =>
            {
                var textTags = CollectTags(r, "span").Where(t => t.Contains("instruction__text", StringComparison.Ordinal));
                return new RecipeInstruction
                {
                    Text = string.Join(" ", textTags.Select(t =>
                    {
                        t = t.Remove(0, t.IndexOf('>') + 1);
                        return t[..t.IndexOf('<')];
                    }))
                };
            }).ToArray()
        };
    }

    private string FindMetaValue(string responseContent, string name)
    {
        foreach (var div in CollectTags(responseContent, "div|class=recipe-meta-property-group__labels"))
        {
            var value = FindTagValue(div, "div|class=recipe-meta-property-group__value").Trim();
            var title = FindTagValue(div, "div|class=recipe-meta-property-group__title").Trim();
            if (title == name)
                return value;
        }
        return string.Empty;
    }
}
