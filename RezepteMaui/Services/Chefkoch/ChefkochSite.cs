using Rezepte.Services.Chefkoch.Models;
using System;
using System.Linq;

namespace Rezepte.Services.Chefkoch
{

    public class ChefkochSite: BaseReceiptSource
    {

        public ChefkochSite()
            : base() { }

        public async Task<Receipt> LoadReceipt(string uri)
        {
            if (!uri.Contains("chefkoch"))
                return null;
            HttpClient client = new HttpClient();
            var responseContent = await client.GetStringAsync(uri);
            var headContent = FindTag(responseContent, false, "head");

            Receipt receipt = new Receipt()
            {
                URI = uri
            };
            receipt.Title = FindTagValue(headContent, "title").Split('|').First();

            responseContent = FindTagValue(responseContent, "body", "main");
            var articles = CollectTags(responseContent, "article");
            if (articles.Length == 0)
                return null;
            receipt.Pictures = await FindPicturesAsync(headContent);

            receipt.Ingredients = FindIngredients(articles);
            if (receipt.Ingredients == null)
                return null;
            receipt.Instructions = FindInstructions(articles);
            if (receipt.Instructions == null)
                return null;
            return receipt;
        }
        public override Task<string[]> ExtractUris(string html)
        {
            List<string> uriList = new List<string>();
            var Links = CollectTags(html, "a");
            return Task.FromResult(Links
                .Select(a =>
                {
                    int offset = a.IndexOf("href=");
                    if (offset < 0)
                        return string.Empty;
                    a = a.Remove(0, offset + "href=".Length);
                    offset = a.IndexOfAny(new char[] { ' ', '>' });
                    a = a.Remove(offset);
                    a = a.Trim(' ', '"','/');
                    return a.ToLower();
                })
                .Where(uri => !string.IsNullOrWhiteSpace(uri))
                .Where(uri => uri.StartsWith("https://www.chefkoch.de/rezepte/"))
                .Where(uri =>!uri.StartsWith("https://www.chefkoch.de/rezepte/was-koche-ich-heute"))
                .Where(uri => !uri.StartsWith("https://www.chefkoch.de/rezepte/was-backe-ich-heute"))
                .Where(uri => !uri.StartsWith("https://www.chefkoch.de/rezepte/kategorien"))
                .ToArray());
        }
        private async Task<byte[][]> FindPicturesAsync(string content)
        {
            var pictureUri = FindTagValue(content, "head", "meta|property=og:image");
            var pictureTempPath = await DownloadFileAsync(pictureUri);
            try
            {
                byte[] imageArray = File.ReadAllBytes(pictureTempPath);
                return new byte[][] { imageArray };
            }
            finally
            {
                File.Delete(pictureTempPath);
            }
        }

        private string FindInstructions(string[] articles)
        {
            var instructionArticle = articles.FirstOrDefault(article => article.Contains("Zubereitung"));
            if (instructionArticle == null)
                return null;
            var box = FindTagValue(instructionArticle, "div");
            return box;
        }

        
        private ReceiptIngredients FindIngredients(string[] articles)
        {
            ReceiptIngredients ingredients = new ReceiptIngredients();
            var ingredientsArticle = articles.FirstOrDefault(article => article.Contains("recipe-ingredients"));
            if (ingredientsArticle == null)
                return null;
            ingredients.Quantity = int.Parse(FindTagValue(ingredientsArticle, "div", "form", "input"));
            var ingredientTable = FindTag(ingredientsArticle, false, "table");
            var rows = CollectTags(ingredientTable, "tr");
            ingredients.Items = rows.Select(row =>
            {
                var header = CollectTags(row, "th");
                var cells = CollectTags(row, "td");
                if (cells.Length > 0)
                {
                    var ingredient = new ReceiptIngredient()
                    {
                        Quantity = FindTagValue(cells.First(), "span"),
                        Name = FindTagValue(cells.Last(), "span", "a")
                    };
                    if (string.IsNullOrWhiteSpace(ingredient.Name))
                        ingredient.Name = FindTagValue(cells.Last(), "span");
                    return ingredient;
                }
                if (header.Length > 0)
                {
                    var ingredient = new ReceiptIngredient()
                    {
                        Quantity = string.Empty,
                        Name = FindTagValue(header.Last(), "h3", "a")
                    };
                    if (string.IsNullOrWhiteSpace(ingredient.Name))
                        ingredient.Name = FindTagValue(header.Last(), "h3");
                    return ingredient;
                }
                return null;
            })
                                    .Where(i => i is not null)
                                    .ToArray();
            return ingredients;
        }

        public override async Task<ISourceReceipt> FromUriAsync(string uri)
        {
            return await LoadReceipt(uri);
        }

    }
}
