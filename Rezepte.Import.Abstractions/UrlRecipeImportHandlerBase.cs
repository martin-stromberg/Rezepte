using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace Rezepte.Import.Abstractions;

public abstract class UrlRecipeImportHandlerBase : ImportParserBase, IImportHandler
{
    private StreamReader? lastReader;
    private KeyValuePair<string, RecipeImport[]> lastRecipe;

    public string UserId { protected get; set; } = string.Empty;

    protected sealed class RecipeImport
    {
        public string? Title { get; set; }
        public byte[][]? Pictures { get; set; }
        public RecipeIngredients? Ingredients { get; set; }
        public RecipeInstructions? Instructions { get; set; }
        public string? Description { get; set; }
        public string? Uri { get; set; }
        public int Portions { get; set; }
        public int WorkTime { get; set; }
    }

    protected sealed class RecipeInstructions
    {
        public RecipeInstruction[]? Steps { get; set; }
    }

    protected sealed class RecipeInstruction
    {
        public string? Text { get; set; }
    }

    protected sealed class RecipeIngredients
    {
        public RecipeIngredient?[]? Items { get; set; }
        public int Quantity { get; set; }
    }

    protected sealed class RecipeIngredient
    {
        public string? Quantity { get; set; }
        public string? Name { get; set; }
    }

    protected string FindTag(string content, bool removeTag, params string[] tags)
    {
        for (var idx = 0; idx < tags.Length; idx++)
        {
            var tag = tags[idx];
            var tagParts = tag.Split('|');
            var tagName = tagParts.First();
            var isLast = idx == tags.Length - 1;
            while (!string.IsNullOrWhiteSpace(content))
            {
                var offset = content.IndexOf($"<{tagName}", StringComparison.Ordinal);
                if (offset < 0)
                    return string.Empty;
                content = content.Remove(0, offset);
                var tagStart = content.Remove(content.IndexOf('>') + 1);
                if (tagStart != $"<{tagName}>" && !tagStart.StartsWith($"<{tagName} ", StringComparison.Ordinal))
                {
                    content = content.Remove(0, tagStart.Length);
                    continue;
                }

                if (tagParts.Length > 1)
                {
                    var tagFound = !tagParts.Skip(1).Any(t =>
                    {
                        var args = t.Split('=');
                        var doubleQuoteOffset = tagStart.IndexOf($"{args[0]}=\"", StringComparison.Ordinal);
                        var singleQuoteOffset = tagStart.IndexOf($"{args[0]}='", StringComparison.Ordinal);
                        if (doubleQuoteOffset < 0 && singleQuoteOffset < 0)
                            return true;
                        var attrOffset = doubleQuoteOffset < 0 ? singleQuoteOffset : doubleQuoteOffset;
                        var param = tagStart.Remove(0, attrOffset + args[0].Length + 2);
                        var endOffset = singleQuoteOffset >= 0 ? param.IndexOf('\'') : param.IndexOf('"');
                        param = param.Remove(endOffset);
                        return param != args[1];
                    });
                    if (!tagFound)
                    {
                        content = content.Remove(0, tagStart.Length);
                        continue;
                    }
                }

                if (tagStart.EndsWith("/>", StringComparison.Ordinal) || !content.Contains($"</{tagName}", StringComparison.Ordinal))
                    return GetTagAttribute(tagStart, "value|content|src|href");
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
        var offset = content.IndexOf($"</{tags.Last().Split('|').First()}", StringComparison.Ordinal);
        if (offset >= 0)
            content = content.Remove(offset);
        return content.Replace("<br>", "\r\n").Trim();
    }

    protected string[] CollectTagValues(string content, string tagName)
    {
        var tags = new List<string>();
        while (content.Length > 0)
        {
            var tag = FindTag(content, false, tagName);
            if (string.IsNullOrWhiteSpace(tag))
                break;
            tags.Add(tag);
            content = content.Remove(0, content.IndexOf(tag, StringComparison.Ordinal));
            content = content.Remove(0, tag.Length);
        }
        return tags.ToArray();
    }

    protected TimeSpan ParseGermanTimeSpan(string input)
    {
        var match = Regex.Match(input, @"(?:(\d+)\s*Std\.)?\s*(?:(\d+)\s*Min\.)?");
        if (!match.Success)
            return TimeSpan.Zero;

        var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        var minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        return new TimeSpan(hours, minutes, 0);
    }

    protected virtual string FindUrl(string html)
    {
        var headContent = FindTag(html, false, "head");
        return WebUtility.HtmlDecode(FindTagValue(headContent, "meta|property=og:url").Split('|').First());
    }

    protected virtual string FindTitle(string html)
    {
        var headContent = FindTag(html, false, "head");
        var title = WebUtility.HtmlDecode(FindTagValue(headContent, "title").Split('|').First());
        if (string.IsNullOrWhiteSpace(title))
            title = WebUtility.HtmlDecode(FindTagValue(headContent, "meta|name=og:title").Split('|').First());
        return title;
    }

    protected virtual async Task<byte[][]?> FindPicturesAsync(string html)
    {
        var pictureUri = FindTagValue(html, "head", "meta|property=og:image");
        if (string.IsNullOrWhiteSpace(pictureUri))
            return null;
        var imageArray = await DownloadImageAsync(pictureUri.Trim('\'', '"')).ConfigureAwait(false);
        return imageArray is { Length: > 0 } ? [imageArray] : null;
    }

    protected async Task<byte[]?> DownloadImageAsync(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        var pictureTempPath = await DownloadFileAsync(uri.Trim('\'', '"')).ConfigureAwait(false);
        try
        {
            return File.ReadAllBytes(pictureTempPath);
        }
        catch
        {
            return null;
        }
        finally
        {
            File.Delete(pictureTempPath);
        }
    }

    protected XmlElement? CreateNode(string code)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(code);
        return xmlDoc.DocumentElement;
    }

    protected string GetNodeText(XmlNode node)
    {
        if (node is XmlComment)
            return string.Empty;
        if (node.ChildNodes.Count == 0)
            return node.InnerText;
        return string.Join(" ", node.ChildNodes.Cast<XmlNode>().Select(GetNodeText)).Replace("  ", " ").Trim();
    }

    protected IEnumerable<string> CollectScriptContents(string html)
    {
        foreach (var script in CollectTags(html, "script"))
        {
            var match = Regex.Match(script, @"<script\b[^>]*?(?:type\s*=\s*[""']application/ld\+json[""'])?[^>]*>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;
            var scriptContent = match.Groups[1].Value.Replace("\r", "").Replace("\n", "");
            if (!string.IsNullOrWhiteSpace(scriptContent))
                yield return scriptContent;
        }
    }

    protected string[] CollectTags(string content, string tagName)
    {
        var tagBase = tagName.Split('|').First();
        var startTagPattern = $@"<{tagBase}(\s[^>]*)?>";
        var endTag = $"</{tagBase}>";
        var tags = new List<string>();

        while (!string.IsNullOrEmpty(content))
        {
            var matchStart = Regex.Match(content, startTagPattern);
            if (!matchStart.Success)
                break;

            var startIndex = matchStart.Index;
            var currentIndex = startIndex;
            var level = 0;

            do
            {
                var nextStart = Regex.Match(content[currentIndex..], startTagPattern);
                var nextEnd = content.IndexOf(endTag, currentIndex, StringComparison.Ordinal);
                if (nextStart.Success && nextEnd >= 0 && nextStart.Index + currentIndex < nextEnd)
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
                    break;
                }
            }
            while (level > 0);

            var length = currentIndex - startIndex;
            if (length <= 0)
                break;
            tags.Add(content.Substring(startIndex, length));
            content = content[(startIndex + length)..];
        }

        return tags.ToArray();
    }

    protected async Task<string> DownloadFileAsync(string uri)
    {
        var tempFilePath = Path.GetTempFileName();
        using var client = new HttpClient();
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        try
        {
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException($"Error in download: {response.StatusCode}");

            await using var streamToReadFrom = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using var output = new FileStream(tempFilePath, FileMode.Create);
            await streamToReadFrom.CopyToAsync(output).ConfigureAwait(false);
        }
        catch
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
            throw;
        }
        return tempFilePath;
    }

    protected abstract Task<KeyValuePair<string, RecipeImport[]>> ReadSingleRecipe(string fileName, string responseContent);

    protected virtual Task<string[]> ExtractRecipeUriCollection(string html) => Task.FromResult(Array.Empty<string>());

    protected virtual async Task<KeyValuePair<string, RecipeImport[]>> ReadRecipeCollection(string fileName, string responseContent, CancellationToken ct)
    {
        var results = new List<RecipeImport>();
        foreach (var uri in await ExtractRecipeUriCollection(responseContent).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var tempFilePath = await DownloadFileAsync(uri).ConfigureAwait(false);
                try
                {
                    var singleContent = await File.ReadAllTextAsync(tempFilePath, ct).ConfigureAwait(false);
                    var singleRecipe = await ReadSingleRecipe(uri, singleContent).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(singleRecipe.Key) && singleRecipe.Value.Length > 0)
                        results.AddRange(singleRecipe.Value);
                }
                finally
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // Continue with remaining collection links.
            }
        }
        return new KeyValuePair<string, RecipeImport[]>(fileName, results.ToArray());
    }

    public async Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        lastReader?.Dispose();
        lastReader = new StreamReader(stream);
        try
        {
            var responseContent = await lastReader.ReadToEndAsync(ct).ConfigureAwait(false);
            lastRecipe = await ReadSingleRecipe(fileName, responseContent).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(lastRecipe.Key))
                lastRecipe = await ReadRecipeCollection(fileName, responseContent, ct).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(lastRecipe.Key) && lastRecipe.Value.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(new ImportResult(false, "filename is required", []));

        if (lastRecipe.Key != fileName || lastRecipe.Value.Length == 0)
            return Task.FromResult(new ImportResult(false, "No cached parse result for this filename. Call CanHandleAsync first.", []));

        lastReader?.Dispose();
        lastReader = null;
        return Task.FromResult(new ImportResult(true, null, [], lastRecipe.Value.Select(ToImportedRecipe).ToList()));
    }

    private static ImportedRecipe ToImportedRecipe(RecipeImport imported)
    {
        return new ImportedRecipe
        {
            Title = imported.Title,
            Description = imported.Description,
            SourceUri = imported.Uri,
            Portions = imported.Portions,
            WorkTimeMinutes = imported.WorkTime,
            Ingredients = (imported.Ingredients?.Items ?? [])
                .Where(i => i is not null)
                .Select(i => new ImportedIngredient { Quantity = i!.Quantity, Name = i.Name })
                .ToList(),
            Steps = (imported.Instructions?.Steps ?? [])
                .Select(s => new ImportedRecipeStep { Text = s.Text })
                .ToList(),
            Images = (imported.Pictures ?? [])
                .Where(p => p is { Length: > 0 })
                .Select((p, index) =>
                {
                    var extension = GetImageExtension(p);
                    return new ImportedImage
                    {
                        Data = p,
                        FileName = SanitizeFileName($"{imported.Title ?? "image"}-{index + 1}{extension}"),
                        ContentType = GetContentTypeFromExtension(extension)
                    };
                })
                .ToList()
        };
    }

    private static string GetTagAttribute(string tag, string valueNames)
    {
        foreach (var valueName in valueNames.Split('|'))
        {
            var offset = tag.IndexOf($"{valueName}=", StringComparison.Ordinal);
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

            offset = tag.IndexOfAny([' ', '>']);
            if (offset < 0)
                return string.Empty;
            tag = tag.Remove(offset);
            return tag.TrimEnd('/').Trim('"');
        }
        return string.Empty;
    }

    private static string GetImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 4)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return ".jpg";
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) return ".gif";
        }
        return ".bin";
    }

    private static string GetContentTypeFromExtension(string ext)
    {
        return (ext ?? string.Empty).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "file";
        foreach (var c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');
        return input;
    }
}
