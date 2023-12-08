using Rezepte.Services.Chefkoch.Models;
using System;
using System.Linq;

namespace Rezepte.Services.Chefkoch
{
    public class ChefkochSite
    {

        public async Task<Receipt> LoadReceipt(string uri)
        {
            HttpClient client = new HttpClient();
            var responseContent = await client.GetStringAsync(uri);

            Receipt receipt = new Receipt();
            receipt.Title = FindTagValue(responseContent, "head", "title");

            responseContent = FindTagValue(responseContent, "body", "main");
            var articles = CollectTags(responseContent, "article");
            receipt.Ingredients = FindIngredients(articles);
            receipt.Instructions = FindInstructions(articles);
            return receipt;
        }

        private string FindInstructions(string[] articles)
        {
            var instructionArticle = articles.FirstOrDefault(article => article.Contains("Zubereitung"));
            var box = FindTagValue(instructionArticle, "div");
            return box;
        }

        private string FindTag(string content, bool removeTag, params string[] tags)
        {
            for (int idx = 0; idx < tags.Length; idx++)
            {
                var tag = tags[idx];
                var isLast = idx == (tags.Length - 1);
                int offset = content.IndexOf($"<{tag}");
                if (offset < 0)
                    return string.Empty;
                content = content.Remove(0, offset);
                var tagStart = content.Remove(content.IndexOf(">") + 1);
                if (tagStart.EndsWith("/>") || !content.Contains($"</{tag}"))
                    return GetTagAttribute(tagStart, "value");
                if (!isLast || removeTag)
                    content = content.Remove(0, tagStart.Length);
            }
            return content;
        }

        private string GetTagAttribute(string tag, string valueName)
        {
            int offset = tag.IndexOf($"{valueName}=");
            if (offset < 0)
                return string.Empty;
            tag = tag.Remove(0, offset + valueName.Length + 1);

            offset = tag.IndexOfAny(new char[] { ' ', '/', '>' });
            if (offset < 0)
                return string.Empty;
            tag = tag.Remove(offset);
            return tag.Trim('"');
        }

        private string FindTagValue(string content, params string[] tags)
        {
            content = FindTag(content, true, tags);
            int offset = content.IndexOf($"</{tags.Last()}");
            if (offset >= 0)
                content = content.Remove(offset);
            return content.Replace("<br>", "\r\n").Trim();
        }

        private string[] CollectTags(string content, string tagName)
        {
            var endTag = $"</{tagName}>";
            List<string> tags = new List<string>();
            while (content.Length > 0)
            {
                var tag = FindTag(content, false, tagName);
                if (string.IsNullOrWhiteSpace(tag))
                    break;
                tag = tag.Remove(tag.IndexOf(endTag) + endTag.Length);
                tags.Add(tag);

                content = content.Remove(0, content.IndexOf(tag));
                content = content.Remove(0, tag.Length);
            }
            return tags.ToArray();
        }

        private ReceiptIngredients FindIngredients(string[] articles)
        {
            ReceiptIngredients ingredients = new ReceiptIngredients();
            var ingredientsArticle = articles.FirstOrDefault(article => article.Contains("recipe-ingredients"));
            var ingredientTable = FindTag(ingredientsArticle, false, "table", "tbody");
            var rows = CollectTags(ingredientTable, "tr");

            ingredients.Quantity = int.Parse(FindTagValue(ingredientsArticle, "div", "form", "input"));
            ingredients.Items = rows.Select(row =>
            {
                var cells = CollectTags(row, "td");
                var ingredient = new ReceiptIngredient()
                {
                    Quantity = FindTagValue(cells.First(), "span"),
                    Name = FindTagValue(cells.Last(), "span", "a")
                };
                if (string.IsNullOrWhiteSpace(ingredient.Name))
                    ingredient.Name = FindTagValue(cells.Last(), "span");
                return ingredient;
            })
                                    .ToArray();

            return ingredients;
        }

    }
}
