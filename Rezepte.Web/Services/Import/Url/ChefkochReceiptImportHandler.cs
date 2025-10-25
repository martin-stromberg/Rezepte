using Microsoft.EntityFrameworkCore.Update;
using Rezepte.Web.Entities;
using System;
using System.Formats.Asn1;
using System.Linq;
using System.Xml;
using static Google.Cloud.Vision.V1.ProductSearchResults.Types;
using static Rezepte.Web.Services.Import.GeminiClient;
using static System.Collections.Specialized.BitVector32;

namespace Rezepte.Web.Services.Import.Url;
public class ChefkochReceiptImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public ChefkochReceiptImportHandler(IRecipeService recipes, ILogger<ChefkochReceiptImportHandler> logger) : base (recipes, logger)
    {
    }
    protected override string FindTitle(string html)
    {
        html = FindTagValue(html, "body", "main");
        var contentTitle = FindTagValue(html, "h1");
        if (!string.IsNullOrWhiteSpace(contentTitle))
            return contentTitle;
        return base.FindTitle(html);
    }
    protected override async Task<byte[][]> FindPicturesAsync(string html)
    {
        var tags = CollectTags(html, "div|class=ds-slider__item").Where(t => t.Contains("ds-slider-image__image-wrap")).SelectMany(t => CollectTagValues(t, "img")).ToArray();
        List<byte[]> imageDataCollection = new List<byte[]>();
        foreach (var imageUri in tags)
            try
            {
                var pictureTempPath = await DownloadFileAsync(imageUri);
                try
                {
                    byte[] imageArray = File.ReadAllBytes(pictureTempPath);
                    imageDataCollection.Add(imageArray);
                }
                finally
                {
                    File.Delete(pictureTempPath);
                }
            }
            catch { }
        return imageDataCollection.Concat(await base.FindPicturesAsync(html)).ToArray();
    }
    private RecipeIngredients FindIngredients(string[] articles)
    {
        RecipeIngredients ingredients = new RecipeIngredients();
        var ingredientsArticle = articles.FirstOrDefault(article => article.Contains("recipe-ingredients"));
        if (ingredientsArticle == null)
            return null;
        ingredients.Quantity = int.Parse(FindTagValue(ingredientsArticle, "input"));
        var ingredientTable = FindTag(ingredientsArticle, false, "table");
        var rows = CollectTags(ingredientTable, "tr");
        ingredients.Items = rows.Select(row =>
        {
            var node = CreateNode(row);
            var quantityCell = node.ChildNodes.OfType<XmlNode>().FirstOrDefault(n => n.Name == "td");
            var nameCell = node.ChildNodes.OfType<XmlNode>().Skip(1).FirstOrDefault(n => n.Name == "td");

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
        return ingredients;
    }
    private string FindInstructions(string[] articles)
    {
        var instructionArticle = articles.FirstOrDefault(article => article.Contains("Zubereitung"));
        if (instructionArticle == null)
            return null;
        var tags = CollectTags(instructionArticle, "div");
        var instructionRows = tags.Where(r => r.Contains("instruction-row")).ToList();
        return string.Join("\r\n", instructionRows.Select(r =>
        {
            var textTags = CollectTags(r, "span").Where(t => t.Contains("instruction__text"));
            return string.Join(" ", textTags.Select(t =>
            {
                t = t.Remove(0, t.IndexOf(">") + 1);
                t = t.Substring(0, t.IndexOf("<"));
                return t;
            }));
        }));
    }
    private string FindMetaValue(string responseContent, string name = "")
    {
        var labels = CollectTags(responseContent, "div|class=recipe-meta-property-group__labels");
        foreach (var div in labels)
        {
            var value = FindTagValue(div, "div|class=recipe-meta-property-group__value").Trim();
            var title = FindTagValue(div, "div|class=recipe-meta-property-group__title").Trim();
            if (title == name)
                return value;
        }
        return string.Empty;
    }

    protected override async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            var headContent = FindTag(responseContent, false, "head");

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
            if (recipe.Instructions == null)
                throw new ApplicationException("no instructions");

            recipe.WorkTime = (int)ParseGermanTimeSpan(FindMetaValue(responseContent, "Arbeitszeit")).TotalMinutes;

            return new KeyValuePair<string, RecipeImport[]>(fileName, new RecipeImport[] { recipe });
        }
        catch
        {
            return new KeyValuePair<string, RecipeImport[]>();
        }
    }
}