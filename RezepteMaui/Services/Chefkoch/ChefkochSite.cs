using Rezepte.Services.Chefkoch.Models;
using System;
using System.Linq;

namespace Rezepte.Services.Chefkoch
{
    public interface IReceiptSourceSettings
    {

        string TempFolderPath { get; }

    }

    public class ReceiptSourceSettings: IReceiptSourceSettings
    {

        public string TempFolderPath => throw new NotImplementedException();

    }

    public abstract class BaseReceiptSource: IReceiptSource
    {

        private readonly IReceiptSourceSettings _Settings;

        public BaseReceiptSource(IReceiptSourceSettings settings)
        {
            _Settings = settings;
        }

        public abstract Task<ISourceReceipt> FromUriAsync(string uri);

        protected async Task<string> DownloadFileAsync(string uri)
        {
            var tempFilePath = Path.GetTempFileName();
            using (HttpClient client = new HttpClient())
                using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)
                                                  .ConfigureAwait(false))
                    try
                    {
                        if (!response.IsSuccessStatusCode)
                            throw new ApplicationException($"Error in download: {response.StatusCode}");

                        var total = response.Content.Headers.ContentLength ?? -1L;
                        double progress = 0;

                        using (var streamToReadFrom = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var totalRead = 0L;
                            var buffer = new byte[2048];
                            var isMoreToRead = true;
                            var fileWriteTo = tempFilePath;
                            var output = new FileStream(fileWriteTo, FileMode.Create);
                            do
                            {
                                var read = await streamToReadFrom.ReadAsync(buffer,
                                                                            0,
                                                                            buffer.Length,
                                                                            CancellationToken.None);

                                if (read == 0)
                                    isMoreToRead = false;
                                else
                                {
                                    await output.WriteAsync(buffer, 0, read);

                                    totalRead += read;

                                    progress = ((totalRead * 1d) / (total * 1d)) * 100;
                                }
                            }
                            while (isMoreToRead);

                            output.Close();
                        }
                    }
                    catch
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                        throw;
                    }
            return tempFilePath;
        }

    }

    public class ChefkochSite: BaseReceiptSource
    {

        public ChefkochSite(IReceiptSourceSettings settings)
            : base(settings) { }

        public async Task<Receipt> LoadReceipt(string uri)
        {
            HttpClient client = new HttpClient();
            var responseContent = await client.GetStringAsync(uri);
            var headContent = FindTag(responseContent, false, "head");

            Receipt receipt = new Receipt();
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

        private string FindTag(string content, bool removeTag, params string[] tags)
        {
            for (int idx = 0; idx < tags.Length; idx++)
            {
                var tag = tags[idx];
                var tagParts = tag.Split('|');
                var tagName = tagParts.First();
                var isLast = idx == (tags.Length - 1);
                while (!string.IsNullOrWhiteSpace(content))
                {
                    int offset = content.IndexOf($"<{tagName}");
                    if (offset < 0)
                        return string.Empty;
                    content = content.Remove(0, offset);
                    var tagStart = content.Remove(content.IndexOf(">") + 1);

                    if (tagParts.Length > 1)
                    {
                        var tagFound = !tagParts.Skip(1)
                                                .Any(t =>
                                                {
                                                    // Die erste Eigenschaft suchen, die nicht passt:
                                                    var args = t.Split('=');
                                                    int offset = tagStart.IndexOf($"{args[0]}=\"");
                                                    if (offset < 0)
                                                        return true;
                                                    var param = tagStart.Remove(0, offset + args[0].Length + 2);
                                                    param = param.Remove(param.IndexOf('\"'));
                                                    return param != args[1];
                                                });
                        if (!tagFound)
                        {
                            content = content.Remove(0, tagStart.Length);
                            continue;
                        }
                    }

                    if (tagStart.EndsWith("/>") || !content.Contains($"</{tagName}"))
                        return GetTagAttribute(tagStart, "value|content");
                    if (!isLast || removeTag)
                        content = content.Remove(0, tagStart.Length);
                    break;
                }
            }
            return content;
        }

        private string GetTagAttribute(string tag, string valueNames)
        {
            var valueNameParts = valueNames.Split('|');
            foreach (var valueName in valueNameParts)
            {
                int offset = tag.IndexOf($"{valueName}=");
                if (offset < 0)
                    continue;
                tag = tag.Remove(0, offset + valueName.Length + 1);

                offset = tag.IndexOfAny(new char[] { ' ', '>' });
                if (offset < 0)
                    return string.Empty;
                tag = tag.Remove(offset);
                return tag.TrimEnd('/').Trim('"');
            }
            return string.Empty;
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
            if (ingredientsArticle == null)
                return null;
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

        public override async Task<ISourceReceipt> FromUriAsync(string uri)
        {
            return await LoadReceipt(uri);
        }

    }
}
