using Rezepte.Import.Abstractions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

public sealed class ChefkochImportHandler : UrlRecipeImportHandlerBase, ICollectionImportHandler
{
    private static readonly Regex RecipeLinkRegex = new(
        @"<a\b(?<attrs>[^>]*\bhref\s*=\s*[""'](?<href>[^""']*?/rezepte/(?<id>\d+)[^""']*?\.html(?:\?[^""']*)?)[""'][^>]*)>(?<text>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ImageRegex = new(
        @"<img\b[^>]*\bsrc\s*=\s*[""'](?<src>[^""']+)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public async Task<ImportCollectionPreview?> TryReadCollectionPreviewAsync(Stream stream, string fileName, string? uri, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var html = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        if (!LooksLikeCollection(uri, html))
        {
            return null;
        }

        var items = ExtractCollectionItems(html, uri).ToArray();
        if (items.Length == 0)
        {
            return null;
        }

        var sourceUri = NormalizeUrl(uri, uri);
        return new ImportCollectionPreview(
            Id: CreateCollectionId(sourceUri ?? fileName),
            Title: FindCollectionTitle(html),
            SourceUri: sourceUri,
            Items: items);
    }

    public async Task<ImportResult> ImportCollectionItemAsync(ImportCollectionItem item, string userId, CancellationToken ct = default)
    {
        try
        {
            var tempFilePath = await DownloadFileAsync(item.Url).ConfigureAwait(false);
            try
            {
                var html = await File.ReadAllTextAsync(tempFilePath, ct).ConfigureAwait(false);
                var singleRecipe = await ReadSingleRecipe(item.Url, html).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(singleRecipe.Key) || singleRecipe.Value.Length == 0)
                {
                    return new ImportResult(false, "Das Rezept konnte nicht gelesen werden.", []);
                }

                return new ImportResult(true, null, [], singleRecipe.Value.Select(ToImportedRecipe).ToList());
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
        catch (Exception ex)
        {
            return new ImportResult(false, ex.Message, []);
        }
    }

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

    private static bool LooksLikeCollection(string? uri, string html)
    {
        if (!string.IsNullOrWhiteSpace(uri)
            && uri.Contains("/rezeptsammlung/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return html.Contains("/rezeptsammlung/", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Rezeptsammlung", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<ImportCollectionItem> ExtractCollectionItems(string html, string? baseUri)
    {
        var byUrl = new Dictionary<string, ImportCollectionItem>(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in RecipeLinkRegex.Matches(html))
        {
            var url = NormalizeUrl(match.Groups["href"].Value, baseUri);
            if (string.IsNullOrWhiteSpace(url) || byUrl.ContainsKey(url))
            {
                continue;
            }

            var title = CleanText(match.Groups["text"].Value);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Chefkoch-Rezept";
            }

            var surrounding = html.Substring(Math.Max(0, match.Index - 600), Math.Min(html.Length - Math.Max(0, match.Index - 600), match.Length + 1200));
            var thumbnail = NormalizeUrl(ImageRegex.Match(surrounding).Groups["src"].Value, baseUri);
            var id = match.Groups["id"].Success ? $"chefkoch-{match.Groups["id"].Value}" : CreateCollectionId(url);
            if (!seenIds.Add(id))
            {
                continue;
            }

            byUrl[url] = new ImportCollectionItem(id, title, url, thumbnail);
        }

        return byUrl.Values;
    }

    private static string? FindCollectionTitle(string html)
    {
        var h1 = Regex.Match(html, @"<h1\b[^>]*>(?<text>.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (h1.Success)
        {
            var title = CleanText(h1.Groups["text"].Value);
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }

        var titleMatch = Regex.Match(html, @"<title\b[^>]*>(?<text>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return titleMatch.Success ? CleanText(titleMatch.Groups["text"].Value) : null;
    }

    private static string CleanText(string html)
    {
        var text = Regex.Replace(html, "<.*?>", " ");
        text = WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string? NormalizeUrl(string? value, string? baseUri)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = WebUtility.HtmlDecode(value.Trim());
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return RemoveQueryAndFragment(absolute).ToString();
        }

        if (!string.IsNullOrWhiteSpace(baseUri)
            && Uri.TryCreate(baseUri, UriKind.Absolute, out var baseAbsolute)
            && Uri.TryCreate(baseAbsolute, value, out var combined))
        {
            return RemoveQueryAndFragment(combined).ToString();
        }

        if (value.StartsWith('/'))
        {
            return Uri.TryCreate($"https://www.chefkoch.de{value}", UriKind.Absolute, out var chefkochUri)
                ? RemoveQueryAndFragment(chefkochUri).ToString()
                : null;
        }

        return null;
    }

    private static Uri RemoveQueryAndFragment(Uri uri)
    {
        if (string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment))
        {
            return uri;
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }

    private static string CreateCollectionId(string value)
    {
        return "chefkoch-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
    }
}
