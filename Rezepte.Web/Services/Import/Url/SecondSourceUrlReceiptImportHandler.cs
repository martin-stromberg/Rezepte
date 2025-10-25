using System.Xml;

namespace Rezepte.Web.Services.Import.Url;

public class SecondSourceUrlReceiptImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public SecondSourceUrlReceiptImportHandler(IRecipeService recipes, ILogger<ChefkochReceiptImportHandler> logger) : base (recipes, logger)
    {
    }

    protected override async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            RecipeImport recipe = new RecipeImport();
            recipe.Title = FindTitle(responseContent);
            recipe.Uri = FindUrl(responseContent);
            recipe.Pictures = await FindPicturesAsync(responseContent);

            responseContent = FindTagValue(responseContent, "body", "main");
            var articles = CollectTags(responseContent, "section");
            if (articles.Length == 0)
                throw new ApplicationException("no sections");
            recipe.Ingredients = FindIngredients(articles);
            if (recipe.Ingredients == null)
                throw new ApplicationException("no ingredients");
            recipe.Instructions = FindInstructions(articles);
            if (string.IsNullOrWhiteSpace(recipe.Instructions))
                throw new ApplicationException("no instructions");

            recipe.WorkTime = (int)ParseGermanTimeSpan(FindMetaValue(articles)).TotalMinutes;

            return new KeyValuePair<string, RecipeImport[]>(fileName, new RecipeImport[] { recipe });
        }
        catch
        {
            return new KeyValuePair<string, RecipeImport[]>();
        }
    }

    private string FindMetaValue(string[] articles)
    {
        List<KeyValuePair<int, string>> resultSet = new List<KeyValuePair<int, string>>();
        foreach (var section in articles)
        {
            var innerSections = CollectTags(section, "section").Where(d => d.Contains("recipe-duration-text"));
            foreach (var innerSection in innerSections)
            {
                var table = FindTag(innerSection, false, "ul");
                var rows = CollectTags(table, "li");
                var currentItems = rows.Select(row =>
                {
                    var node = CreateNode(row);
                    if (node.InnerText.Contains("Vorbereitungszeit"))
                    {
                        var timeText = node.InnerText.Replace("Vorbereitungszeit", "").Trim();
                        return new KeyValuePair<int, string>(1, timeText);
                    }
                    if (node.InnerText.Contains("Zubereitungszeit"))
                    {
                        var timeText = node.InnerText.Replace("Zubereitungszeit", "").Trim();
                        return new KeyValuePair<int, string>(2, timeText);
                    }
                    if (node.InnerText.Contains("Gesamtzeit"))
                    {
                        var timeText = node.InnerText.Replace("Gesamtzeit", "").Trim();
                        return new KeyValuePair<int, string>(99, timeText);
                    }                    
                    return new KeyValuePair<int, string>(0, null);
                })
                                        .Where(i => i.Key !=  0)
                                        .ToArray();
                resultSet.AddRange(currentItems);
            }
        }
        return resultSet.OrderByDescending(i => i.Key).FirstOrDefault().Value ?? string.Empty;
    }

    private string FindInstructions(string[] articles)
    {
        var result = "";        
        foreach (var section in articles)
        {
            var innerSections = CollectTags(section.Remove(0, 1), "section").Where(d => d.Substring(0, d.IndexOf(">")).Contains("recipe-steps"));
            foreach (var innerSection in innerSections)
            {
                var node = CreateNode(innerSection);
                var innerText = ParseNodesTexts(node);
                result += innerText;
            }
            if (innerSections.Any())
                result += "\r\n\r\n";
        }
        return result;
    }

    private string ParseNodesTexts(XmlNode? node)
    {
        if (node.ChildNodes.Count == 0)
            return node.InnerText;
        var result = "";
        foreach (var childNode in node.ChildNodes.OfType<XmlNode>().Where(node => node.Name != "style"))
            result += ParseNodesTexts(childNode) + "\r\n";
        return result;
    }

    private RecipeIngredients FindIngredients(string[] articles)
    {
        var result = new RecipeIngredients() { Items = new RecipeIngredient[0] };
        foreach (var section in articles)
        {
            var innerSections = CollectTags(section, "section").Where(d => d.Contains("recipe-ingredients"));
            foreach (var innerSection in innerSections)
            {
                var sectionTitle = FindTagValue(innerSection, "h5");
                var table = FindTag(innerSection, false, "table");
                var rows = CollectTags(table, "tr");
                var currentItems = rows.Select(row =>
                {
                    var node = CreateNode(row);
                    var quantityCell = node.ChildNodes.OfType<XmlNode>().FirstOrDefault(n => n.Name == "td")?.ChildNodes.OfType<XmlNode>().FirstOrDefault(n => n.Name == "p");
                    var nameCell = node.ChildNodes.OfType<XmlNode>().Skip(1).FirstOrDefault(n => n.Name == "td")?.ChildNodes.OfType<XmlNode>().FirstOrDefault(n => n.Name == "p");
                    if (quantityCell is null || nameCell is null)
                        return null;
                    var ingredient = new RecipeIngredient()
                    {
                        Quantity = quantityCell.InnerText,
                        Name = GetNodeText(nameCell)
                    };
                    return ingredient;
                })
                                        .Where(i => i is not null)
                                        .ToArray();
                result.Items = result.Items.Concat(currentItems).ToArray();
            }
        }
        return result;
    }

    protected override Task<string[]> ExtractRecipeUriCollection(string html)
    {
        return base.ExtractRecipeUriCollection(html);
    }
}
