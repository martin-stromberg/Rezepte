using Microsoft.EntityFrameworkCore.Update;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using static Google.Cloud.Vision.V1.ProductSearchResults.Types;
using static Rezepte.Web.Services.Import.GeminiClient;
using static System.Collections.Specialized.BitVector32;

namespace Rezepte.Web.Services.Import;
public abstract class BaseUrlReceiptImportHandler(IRecipeService recipes, ILogger<UrlReceiptImportHandler> logger): BaseImportHandler
{
    private StreamReader lastReader = null;
    private KeyValuePair<string, RecipeImport[]> _lastRecipe = new KeyValuePair<string, RecipeImport[]>();
    protected sealed class RecipeImport()
    {
        public string Title { get; internal set; }
        public byte[][] Pictures { get; internal set; }
        public RecipeIngredients Ingredients { get; internal set; }
        public string Instructions { get; internal set; }
        public string Description { get; internal set; }
        public string Uri { get; internal set; }
        public int Portions { get; internal set; }
        public int WorkTime { get; internal set; }
    }
    protected sealed class RecipeIngredients()
    {
        public RecipeIngredient?[]? Items { get; internal set; }
        public int Quantity { get; internal set; }
    }
    protected sealed class RecipeIngredient()
    {
        public string Quantity { get; internal set; }
        public string Name { get; internal set; }
    }

    protected string GetTagAttribute(string tag, string valueNames)
    {
        var valueNameParts = valueNames.Split('|');
        foreach (var valueName in valueNameParts)
        {
            int offset = tag.IndexOf($"{valueName}=");
            if (offset < 0)
                continue;
            tag = tag.Remove(0, offset + valueName.Length + 1);
            if (tag.StartsWith('"'))
            {
                tag = tag.TrimStart('"');
                offset = tag.IndexOf('"');
                tag = tag.Remove(offset);
                return tag.Trim('"');
            }
            else
            {
                offset = tag.IndexOfAny(new char[] { ' ', '>' });
                if (offset < 0)
                    return string.Empty;
                tag = tag.Remove(offset);
                return tag.TrimEnd('/').Trim('"');
            }
        }
        return string.Empty;
    }
    protected string FindTag(string content, bool removeTag, params string[] tags)
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
                if (tagStart != $"<{tagName}>" && !tagStart.StartsWith($"<{tagName} "))
                {
                    content = content.Remove(0, tagStart.Length);
                    continue;
                }

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
                    return GetTagAttribute(tagStart, "value|content|src");
                if (!isLast || removeTag)
                    content = content.Remove(0, tagStart.Length);
                break;
            }
        }
        return content;
    }
    protected string FindTagValue(string content, params string[] tags)
    {
        content = FindTag(content, true, tags);
        int offset = content.IndexOf($"</{tags.Last().Split('|').First()}");
        if (offset >= 0)
            content = content.Remove(offset);
        return content.Replace("<br>", "\r\n").Trim();
    }
    protected string[] CollectTagValues(string content, string tagName)
    {
        var startTag = $"<{tagName}>";
        var startTag2 = $"<{tagName} ";
        var endTag = $"</{tagName}>";
        List<string> tags = new List<string>();
        while (content.Length > 0)
        {
            var tag = FindTag(content, false, tagName);
            if (string.IsNullOrWhiteSpace(tag))
                break;
            tags.Add(tag);
            content = content.Remove(0, content.IndexOf(tag));
            content = content.Remove(0, tag.Length);
        }
        return tags.ToArray();
    }
    protected TimeSpan ParseGermanTimeSpan(string input)
    {
        var pattern = @"(?:(\d+)\s*Std\.)?\s*(?:(\d+)\s*Min\.)?";
        var match = Regex.Match(input, pattern);

        if (!match.Success)
            return TimeSpan.Zero;

        int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;

        return new TimeSpan(hours, minutes, 0);
    }
    protected string FindUrl(string html)
    {
        var headContent = FindTag(html, false, "head");
        var url = WebUtility.HtmlDecode(FindTagValue(headContent, "meta|property=og:url").Split('|').First());
        return url;
    }
    protected virtual string FindTitle(string html)
    {
        var headContent = FindTag(html, false, "head");
        var title = WebUtility.HtmlDecode(FindTagValue(headContent, "title").Split('|').First());
        if (string.IsNullOrWhiteSpace(title))
            title = WebUtility.HtmlDecode(FindTagValue(headContent, "meta|name=og:title").Split('|').First());
        return title;
    }
    protected virtual async Task<byte[][]> FindPicturesAsync(string html)
    {
        var pictureUri = FindTagValue(html, "head", "meta|property=og:image");
        if (string.IsNullOrWhiteSpace(pictureUri)) return null;
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
    protected XmlElement? CreateNode(string code)
    {
        System.Xml.XmlDocument XmlDoc = new System.Xml.XmlDocument();
        XmlDoc.LoadXml(code);
        return XmlDoc.DocumentElement;
    }
    protected string GetNodeText(XmlNode node)
    {
        if (node is XmlComment)
            return "";
        if (node.ChildNodes.Count == 0)
            return node.InnerText;
        return string.Join(" ", node.ChildNodes.Cast<XmlNode>().Select(n => GetNodeText(n))).Replace("  ", " ").Trim();
    }

    protected string[] CollectTags(string content, string tagName)
    {
        string tagBase = tagName.Split('|').First();
        string startTagPattern = $@"<{tagBase}(\s[^>]*)?>";
        string endTag = $"</{tagBase}>";
        List<string> tags = new List<string>();

        while (!string.IsNullOrEmpty(content))
        {
            var matchStart = Regex.Match(content, startTagPattern);
            if (!matchStart.Success)
                break;

            int startIndex = matchStart.Index;
            int currentIndex = startIndex;
            int level = 0;

            do
            {
                var nextStart = Regex.Match(content.Substring(currentIndex), startTagPattern);
                var nextEnd = content.IndexOf(endTag, currentIndex, StringComparison.Ordinal);

                if (nextStart.Success && nextStart.Index + currentIndex < nextEnd)
                {
                    level++;
                    currentIndex += nextStart.Index + nextStart.Length;
                }
                else if (nextEnd >= 0)
                {
                    level--;
                    currentIndex = nextEnd + endTag.Length;
                }
                else
                {
                    // Ungültige Struktur: kein passender End-Tag
                    break;
                }
            }
            while (level > 0);

            int length = currentIndex - startIndex;
            string fullTag = content.Substring(startIndex, length);
            tags.Add(fullTag);
            if (startIndex + length == 0)
                break;
            content = content.Substring(startIndex + length);
        }

        return tags.ToArray();
    }
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

    private static string GetImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 4)
        {
            // JPEG: FF D8
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return ".jpg";
            // PNG: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";
            // GIF: 47 49 46 38
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) return ".gif";
        }
        return ".bin";
    }
    private static string GetContentTypeFromExtension(string ext)
    {
        ext = ext?.ToLowerInvariant() ?? string.Empty;
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input)) return "file";
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }
        return input;
    }
    private async Task<bool> CreateNewRecipe(string targetCookbookId, string userId, List<string> created, RecipeImport imported, List<RecipeCreateStep> steps, CancellationToken ct)
    {
        var (ok, error, recipe) = await Recipes.CreateAsync(userId, targetCookbookId, imported.Title ?? "Importiertes Rezept", imported.Description, imported.Uri, imported.Portions, steps, ct).ConfigureAwait(false);
        if (!ok || recipe == null)
        {
            Logger.LogWarning("Failed to create recipe from import: {Title} - {Error}", imported.Title, error);
            return false;
        }

        created.Add(recipe.Id);

        // Attach pictures (if any)
        if (imported.Pictures != null && imported.Pictures.Length > 0)
        {
            for (int i = 0; i < imported.Pictures.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var imgBytes = imported.Pictures[i];
                if (imgBytes == null || imgBytes.Length == 0) continue;

                var ext = GetImageExtension(imgBytes);
                var imgFileName = SanitizeFileName($"{imported.Title ?? "image"}-{i + 1}{ext}");
                var contentType = GetContentTypeFromExtension(ext);
                await Recipes.AddImageAsync(userId, recipe.Id, new MemoryStream(imgBytes), imgFileName, contentType, ct).ConfigureAwait(false);
            }
        }

        return true;
    }

    protected readonly IRecipeService Recipes = recipes;
    protected readonly ILogger<UrlReceiptImportHandler> Logger = logger;
    protected abstract Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent);    
    protected virtual Task<string[]> ExtractRecipeUriCollection(string html)
    {
        return Task.FromResult(new string[0]);
    }
    protected virtual async Task<KeyValuePair<string, RecipeImport[]>> ReadRecipeCollection(string fileName, string responseContent, CancellationToken ct)
    {
        List<RecipeImport> results = new List<RecipeImport>();
        var uris = await ExtractRecipeUriCollection(responseContent);
        foreach (var uri in uris)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var tempFilePath = await DownloadFileAsync(uri);
                try
                {
                    var singleContent = File.ReadAllText(tempFilePath);
                    var singleRecipe = await ReadSingleRecipe(uri, singleContent);
                    if (!string.IsNullOrWhiteSpace(singleRecipe.Key) && singleRecipe.Value != null && singleRecipe.Value.Length > 0)
                    {
                        results.AddRange(singleRecipe.Value);
                    }
                }
                finally
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // ignore errors and continue with next URI
            }
        }
        return new KeyValuePair<string, RecipeImport[]>(fileName, results.ToArray());
    }
    public async Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (lastReader is not null)
            lastReader.Dispose();
        lastReader = new StreamReader(stream);
        try
        {
            var responseContent = lastReader.ReadToEnd();
            _lastRecipe = await ReadSingleRecipe(fileName, responseContent);
            if (string.IsNullOrWhiteSpace(_lastRecipe.Key))
                _lastRecipe = await ReadRecipeCollection(fileName, responseContent, ct);
            if (string.IsNullOrWhiteSpace(_lastRecipe.Key) || _lastRecipe.Value == null || _lastRecipe.Value.Length == 0)
                throw new Exception("no recipes found");
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return new ImportResult(false, "filename is required", new List<string>());

        // Only accept if CanHandleAsync parsed this file earlier and cached result
        if (_lastRecipe.Key != fileName || _lastRecipe.Value == null || _lastRecipe.Value.Length == 0)
        {
            Logger.LogInformation("No cached parse result for {FileName}", fileName);
            return new ImportResult(false, "No cached parse result for this filename. Call CanHandleAsync first.", new List<string>());
        }

        var created = new List<string>();

        foreach (var imported in _lastRecipe.Value)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // Build one step containing the instructions and all ingredients
                var stepIngredients = (imported.Ingredients?.Items ?? Array.Empty<RecipeIngredient>())
                    .Where(i => i is not null)
                    .Select(i =>
                    {
                        // Try to parse numeric amount from the quantity string; fallback to 0
                        decimal amount = 0m;
                        string? unit = null;
                        var qty = i!.Quantity?.Trim() ?? string.Empty;

                        // Simple heuristic: if starts with number, parse until non-number/decimal/comma
                        var numStr = new string(qty.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                        if (!string.IsNullOrWhiteSpace(numStr))
                        {
                            if (decimal.TryParse(numStr.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                            {
                                amount = val;
                                // unit is the rest
                                var rest = qty.Substring(numStr.Length).Trim();
                                unit = string.IsNullOrWhiteSpace(rest) ? null : rest;
                            }
                        }

                        // If no numeric amount parsed, put the whole quantity into the name prefix to preserve info
                        var name = string.IsNullOrWhiteSpace(numStr) ? $"{qty} {i.Name}".Trim() : i.Name;

                        return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, name);
                    }).ToList();

                var steps = new List<RecipeCreateStep>
                {
                    new RecipeCreateStep(
                        Title: null,
                        Description: imported.Instructions ?? string.Empty,
                        DurationMinutes: imported.WorkTime,
                        RequiresOvernightRest: false,
                        Ingredients: stepIngredients)
                };
                var existingRecipe = await Recipes.FindByUri(userId, imported.Uri ?? string.Empty, ct).ConfigureAwait(false);
                if (existingRecipe is not null)
                {
                    var (ok, error) = await Recipes.UpdateAsync(userId, existingRecipe.Id, imported.Title, imported.Description, steps, ct).ConfigureAwait(false);
                    if (!ok)
                    {
                        Logger.LogWarning("Failed to create recipe from import: {Title} - {Error}", imported.Title, error);
                        continue;
                    }

                    // Attach pictures (if any)
                    if (imported.Pictures != null && imported.Pictures.Length > 0)
                    {
                        for (int i = 0; i < imported.Pictures.Length; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var imgBytes = imported.Pictures[i];
                            if (imgBytes == null || imgBytes.Length == 0) continue;

                            var ext = GetImageExtension(imgBytes);
                            var imgFileName = SanitizeFileName($"{imported.Title ?? "image"}-{i + 1}{ext}");
                            var contentType = GetContentTypeFromExtension(ext);
                            await Recipes.AddImageAsync(userId, existingRecipe.Id, new MemoryStream(imgBytes), imgFileName, contentType, ct).ConfigureAwait(false);
                        }
                    }
                    continue;
                }

                bool flowControl = await CreateNewRecipe(targetCookbookId, userId, created, imported, steps, ct).ConfigureAwait(false);
                if (!flowControl)
                {
                    continue;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while importing Chefkoch recipe {FileName}", fileName);
            }
        }

        if (lastReader is not null)
        {
            lastReader.Dispose();
            lastReader = null;
        }
        return new ImportResult(true, null, created);
    }
}
public class SecondSourceUrlReceiptImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public SecondSourceUrlReceiptImportHandler(IRecipeService recipes, ILogger<UrlReceiptImportHandler> logger) : base (recipes, logger)
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
public class UrlReceiptImportHandler : BaseUrlReceiptImportHandler, IImportHandler
{
    public UrlReceiptImportHandler(IRecipeService recipes, ILogger<UrlReceiptImportHandler> logger) : base (recipes, logger)
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