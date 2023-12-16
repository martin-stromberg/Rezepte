using Rezepte.Services.Chefkoch;
using Rezepte.Services.KabelEins.Models;
using System;
using System.Linq;
using System.Net;

namespace Rezepte.Services.KabelEins
{
    public class KabelEinsRezeptSammlung: BaseReceiptSource
    {

        public override async Task<ISourceReceipt> FromUriAsync(string uri)
        {            
            HttpClient client = new HttpClient();
            var responseContent = await client.GetStringAsync(uri);
            var headContent = FindTag(responseContent, false, "head");

            Rezepte.Services.KabelEins.Models.Receipt receipt = new Rezepte.Services.KabelEins.Models.Receipt()
            {
                URI = uri
            };
            receipt.Title = WebUtility.HtmlDecode(FindTagValue(headContent, "title").Split('|').First());            

            responseContent = FindTag(responseContent, false, "body", "div|id=main-content", "article");
            var articles = CollectTags(responseContent, "section");
            if (articles.Length == 0)
                return null;
            receipt.Ingredients = FindIngredients(articles);
            if (receipt.Ingredients == null)
                return null;
            receipt.Instructions = FindInstructions(articles);
            if (receipt.Instructions == null)
                return null;
            receipt.Pictures = await FindPicturesAsync(articles);
            return receipt;
        }
        public override Task<string[]> ExtractUris(string html)
        {
            return Task.FromResult(new string[0]);
        }
        private string FindInstructions(string[] articles)
        {
            string instructions = "";
            var instructionArticle = articles.FirstOrDefault(art => art.Contains("recipe-steps"));
            if (instructionArticle != null)
            {
                var sections = CollectTags(instructionArticle, "section").Where(sec => sec.Contains("recipe-steps")).ToArray();
                foreach (var section in sections)
                {
                    var listItems = CollectTags(section, "li");
                    instructions += string.Join("\r\n", listItems.Select(li =>
                    {
                        var head = FindTagValue(li, "h5", "strong");
                        var description = FindTagValue(li, "p", "span")
                            .Replace("<strong>", "")
                            .Replace("</strong>", "");
                        if (string.IsNullOrWhiteSpace(head) && string.IsNullOrWhiteSpace(description))
                            return string.Empty;
                        return $"{head}\r\n{description}\r\n";
                    }));
                }
            }
            else
            {
                instructionArticle = articles.FirstOrDefault(art => art.Contains("recipe-ingredients"));
                while (instructionArticle.Contains("recipe-ingredients"))
                    instructionArticle = instructionArticle.Remove(0, instructionArticle.IndexOf("recipe-ingredients") + "recipe-ingredients".Length);
                //instructionArticle = instructionArticle.Remove(instructionArticle.IndexOf("/div"));
                var headers = CollectTags(instructionArticle, "h4");
                var descriptions = CollectTags(instructionArticle, "p");
                int offset = 0;
                do
                {
                    var header = headers.Skip(offset).Take(1).FirstOrDefault();
                    var description = descriptions.Skip(offset).Take(1).FirstOrDefault();
                    offset += 1;
                    if (string.IsNullOrWhiteSpace(header) && string.IsNullOrWhiteSpace(description))
                        break;

                    if (header != null)
                    {
                        header = FindTagValue(header, "span");
                        instructions += $"{header}\r\n";
                    }
                    if (description != null)
                    {
                        description = FindTagValue(description, "span");
                        instructions += $"{description}\r\n";
                    }
                } while (true);
            }
            return instructions;
        }

        private ReceiptIngredients FindIngredients(string[] articles)
        {
            ReceiptIngredients ingredients = new ReceiptIngredients();
            ingredients.Items = new ReceiptIngredient[0];
            var ingredientArticle = articles.FirstOrDefault(art => art.Contains("recipe-ingredients"));
            var ingredientSections = CollectTags(ingredientArticle, "section").Where(sec => sec.Contains("recipe-ingredients")).ToArray();
            foreach (var section in ingredientSections)
            {
                var rows = CollectTags(ingredientArticle, "tr")
                    .Where(row => row.Contains("recipe-ingredients-row"))
                    .ToArray();

                var sectionHead = FindTagValue(section, "h5", "span");
                if (!string.IsNullOrWhiteSpace(sectionHead))
                    ingredients.Items = ingredients.Items.Concat(new ReceiptIngredient[] { 
                        new ReceiptIngredient()
                        {
                            Quantity = "",
                            Name = sectionHead
                        }
                    }).ToArray();

                ingredients.Items = ingredients.Items.Concat(rows.Select(row =>
                {
                    var cells = CollectTags(row, "td");
                    var ingredient = new ReceiptIngredient()
                    {
                        Quantity = FindTagValue(cells.First(), "p"),
                        Name = FindTagValue(cells.Last(), "p", "span")
                    };
                    if (string.IsNullOrWhiteSpace(ingredient.Name))
                        ingredient.Name = FindTagValue(cells.Last(), "p");
                    return ingredient;
                })).ToArray();
            }
            return ingredients;
        }

        private async Task<byte[][]> FindPicturesAsync(string[] articles)
        {
            var pictureArticle = articles.FirstOrDefault(art => art.Contains("article-image"));
            pictureArticle = FindTagValue(pictureArticle, "img");
            if (string.IsNullOrWhiteSpace(pictureArticle))
                return null;
            var pictureTempPath = await DownloadFileAsync(pictureArticle);
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
    }
}
