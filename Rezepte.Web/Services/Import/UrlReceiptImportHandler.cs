using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace Rezepte.Web.Services.Import;

public class UrlReceiptImportHandler(IRecipeService recipes, ILogger<UrlReceiptImportHandler> logger) : IImportHandler
{
    private readonly IRecipeService _recipes = recipes;
    private readonly ILogger<UrlReceiptImportHandler> _logger = logger;
    private KeyValuePair<string, RecipeImport[]> _lastRecipe = new KeyValuePair<string, RecipeImport[]>();
    private StreamReader lastReader = null;

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
        }
        return tags.ToArray();
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

            content = content.Substring(startIndex + length);
        }

        return tags.ToArray();
    }

    private async Task<byte[][]> FindPicturesAsync(string content)
    {        
        var pictureUri = FindTagValue(content, "head", "meta|property=og:image");
        //var tags = CollectTagValues(content, "img|class=ds-teaser-link__image").Select(i => i).Concat(new string[] { pictureUri }).Distinct().ToArray();
        var tags = CollectTags(content, "div|class=ds-slider__item").Where(t => t.Contains("ds-slider-image__image-wrap")).SelectMany(t => CollectTagValues(t, "img")).Concat(new string[] { pictureUri }).Distinct().ToArray();
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
        return imageDataCollection.ToArray();
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

    protected XmlElement? CreateNode(string code)
    {
        System.Xml.XmlDocument XmlDoc = new System.Xml.XmlDocument();
        XmlDoc.LoadXml(code);
        return XmlDoc.DocumentElement;
    }
    private string GetNodeText (XmlNode node)
    {
        if (node is XmlComment)
            return "";
        if (node.ChildNodes.Count == 0)
            return node.InnerText;
        return string.Join(" ", node.ChildNodes.Cast<XmlNode>().Select(n => GetNodeText(n))).Replace("  ", " ").Trim();
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
    private sealed class RecipeImport()
    {
        public string Title { get; internal set; }
        public byte[][] Pictures { get; internal set; }
        public RecipeIngredients Ingredients { get; internal set; }
        public string Instructions { get; internal set; }
        public string Description { get; internal set; }
        public string Uri { get; internal set; }
        public int WorkTime { get; internal set; }
    }
    private sealed class RecipeIngredients()
    {
        public RecipeIngredient?[]? Items { get; internal set; }
        public int Quantity { get; internal set; }
    }
    private sealed class RecipeIngredient()
    {
        public string Quantity { get; internal set; }
        public string Name { get; internal set; }
    }
    private Task<string[]> ExtractUris(string html)
    {
        List<string> uriList = new List<string>();
        var Links = CollectTags(html, "a");
        return Task.FromResult(new string[0]);
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

    private async Task<KeyValuePair<string, RecipeImport[]>> ReadRecipeCollection(string fileName, string responseContent, CancellationToken ct)
    {
        List<RecipeImport> results = new List<RecipeImport>();
        var uris = await ExtractUris(responseContent);
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

    private async Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent)
    {
        try
        {
            var headContent = FindTag(responseContent, false, "head");

            RecipeImport recipe = new RecipeImport();
            recipe.Title = WebUtility.HtmlDecode(FindTagValue(headContent, "title").Split('|').First());
            if (string.IsNullOrWhiteSpace(recipe.Title))
                recipe.Title = WebUtility.HtmlDecode(FindTagValue(headContent, "meta|name=og:title").Split('|').First());
            recipe.Uri = WebUtility.HtmlDecode(FindTagValue(headContent, "meta|property=og:url").Split('|').First());

            responseContent = FindTagValue(responseContent, "body", "main");
            var contentTitle = FindTagValue(responseContent, "h1");
            if (!string.IsNullOrWhiteSpace(contentTitle))
                recipe.Title = contentTitle;
            var articles = CollectTags(responseContent, "section");
            if (articles.Length == 0)
                throw new ApplicationException("no sections");

            recipe.Pictures = await FindPicturesAsync(headContent);
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

    public static TimeSpan ParseGermanTimeSpan(string input)
    {
        var pattern = @"(?:(\d+)\s*Std\.)?\s*(?:(\d+)\s*Min\.)?";
        var match = Regex.Match(input, pattern);

        if (!match.Success)
            return TimeSpan.Zero;

        int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;

        return new TimeSpan(hours, minutes, 0);
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

    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return new ImportResult(false, "filename is required", new List<string>());

        // Only accept if CanHandleAsync parsed this file earlier and cached result
        if (_lastRecipe.Key != fileName || _lastRecipe.Value == null || _lastRecipe.Value.Length == 0)
        {
            _logger.LogInformation("No cached parse result for {FileName}", fileName);
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
                var existingRecipe = await _recipes.FindByUri(userId, imported.Uri ?? string.Empty, ct).ConfigureAwait(false);
                if (existingRecipe is not null)
                {
                    var (ok, error) = await _recipes.UpdateAsync(userId, existingRecipe.Id, imported.Title, imported.Description, steps, ct).ConfigureAwait(false);
                    if (!ok)
                    {
                        _logger.LogWarning("Failed to create recipe from import: {Title} - {Error}", imported.Title, error);
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
                            await _recipes.AddImageAsync(userId, existingRecipe.Id, new MemoryStream(imgBytes), imgFileName, contentType, ct).ConfigureAwait(false);
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
                _logger.LogError(ex, "Error while importing Chefkoch recipe {FileName}", fileName);
            }
        }

        if (lastReader is not null)
        {
            lastReader.Dispose();
            lastReader = null;
        }
        return new ImportResult(true, null, created);
    }

    private async Task<bool> CreateNewRecipe(string targetCookbookId, string userId, List<string> created, RecipeImport imported, List<RecipeCreateStep> steps, CancellationToken ct)
    {
        var (ok, error, recipe) = await _recipes.CreateAsync(userId, targetCookbookId, imported.Title ?? "Importiertes Rezept", imported.Description, imported.Uri, steps, ct).ConfigureAwait(false);
        if (!ok || recipe == null)
        {
            _logger.LogWarning("Failed to create recipe from import: {Title} - {Error}", imported.Title, error);
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
                await _recipes.AddImageAsync(userId, recipe.Id, new MemoryStream(imgBytes), imgFileName, contentType, ct).ConfigureAwait(false);
            }
        }

        return true;
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
}