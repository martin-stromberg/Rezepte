using Microsoft.AspNetCore.Components.Authorization;
using Rezepte.Web.Entities;
using System.Formats.Asn1;

namespace Rezepte.Web.Services.Import;

public class AIUrlImportHandler : BaseAIImportHandler, IImportHandler
{
    public AIUrlImportHandler(IRecipeService recipes, ILogger<AIUrlImportHandler> logger, IHttpContextAccessor httpContextAccessor, IAiUsageService aiUsageService)
        :base(httpContextAccessor, aiUsageService)
    {
        _recipes = recipes;
        _logger = logger;
    }
    private struct AIRecipe
    {
        public string Title { get; set; }
        public List<string> Ingredients { get; set; }
        public string Instructions { get; set; }
        public byte[]? ImageData { get; set; }
        public string ImageUri { get; set; }
    }
    private StreamReader lastReader = null;
    private KeyValuePair<string, AIRecipe[]> _lastRecipes;
    private readonly IRecipeService _recipes;
    private readonly ILogger<AIUrlImportHandler> _logger;

    public async Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (lastReader is not null)
            lastReader.Dispose();
        lastReader = new StreamReader(stream);
        try
        {
            var responseContent = lastReader.ReadToEnd();
            _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, await ReadRecipeCollection(fileName, responseContent, ct));
            return _lastRecipes.Value.Any();
        }
        catch
        {
            return false;
        }
    }

    private async Task<AIRecipe[]> ReadRecipeCollection(string fileName, string responseContent, CancellationToken ct)
    {
        GeminiClient client = new GeminiClient(ServicecAcountFile);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url.Request", ct);
        var resultContent = await client.ExtractRecipeFromUrlAsync(responseContent);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Url.Success", ct);
        AIRecipe[] extractedRecipe = ParseRecipes(resultContent);
        return extractedRecipe;
    }

    private AIRecipe[] ParseRecipes(string resultContent)
    {
        return new AIRecipe[] { ParseRecipe(resultContent) };
    }
    private AIRecipe ParseRecipe(string recipeContent)
    {
        AIRecipe extractedRecipe = new AIRecipe();
        extractedRecipe.Title = ParseInformation(recipeContent, "Titel des Rezepts");
        extractedRecipe.Ingredients = ParseInformation(recipeContent, "Zutatenliste").Split("\r\n").Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.TrimStart(' ', '*')).ToList();
        extractedRecipe.Instructions = ParseInformation(recipeContent, "Zubereitungsschritte");
        extractedRecipe.ImageUri = ParseInformation(recipeContent, "Bild-URL");
        if (!string.IsNullOrWhiteSpace(extractedRecipe.ImageUri))
        {
            try
            {
                using var httpClient = new HttpClient();
                var imageData = httpClient.GetByteArrayAsync(extractedRecipe.ImageUri).Result;
                extractedRecipe.ImageData = imageData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image from URL: {ImageUri}", extractedRecipe.ImageUri);
                extractedRecipe.ImageData = null;
            }
        }
        return extractedRecipe;
    }
    private string ParseInformation(string recipeContent, string sectionName)
    {
        return string.Join("\r\n", recipeContent
            .Replace("\r\n", "\n")
            .Replace("\r", "\rn")
            .Split("\n")
            .Select(line => line)
            .SkipWhile(line => !line.StartsWith($"**{sectionName}:**"))
            .Select(line => line.Replace($"**{sectionName}:**", "").Trim())
            .TakeWhile(line => !line.StartsWith("**")
            )).Trim();
    }


    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (_lastRecipes.Key != fileName)
            throw new InvalidOperationException("CanHandleAsync must be called and return true before HandleAsync.");
        var importedRecipes = _lastRecipes.Value;
        var created = new List<string>();
        try
        {
            foreach (var importedRecipe in importedRecipes)
            {
                if (string.IsNullOrWhiteSpace(importedRecipe.Title) && (importedRecipe.Ingredients == null || importedRecipe.Ingredients.Count == 0) && string.IsNullOrWhiteSpace(importedRecipe.Instructions))
                    continue;
                ct.ThrowIfCancellationRequested();

                // Build ingredient list from parsed text (supports mixed numbers, fractions like "1/2" and unicode fractions like "½")
                var stepIngredients = (importedRecipe.Ingredients ?? new List<string>())
                    .Select(line => ParseIngredientLine(line))
                    .ToList();

                var steps = new List<RecipeCreateStep>
                {
                    new RecipeCreateStep(
                        Title: null,
                        Description: importedRecipe.Instructions ?? string.Empty,
                        DurationMinutes: 0,
                        RequiresOvernightRest: false,
                        Ingredients: stepIngredients)
                };

                var (ok, error, recipe) = await _recipes.CreateAsync(userId, targetCookbookId, importedRecipe.Title ?? "Importiertes Fotorezept", null, null, steps, ct).ConfigureAwait(false);
                if (!ok || recipe == null)
                {
                    _logger.LogWarning("Failed to create recipe from AI import: {Title} - {Error}", importedRecipe.Title, error);
                    return new ImportResult(false, error ?? "Failed to create recipe", created);
                }

                created.Add(recipe.Id);

                // Attach scanned photo as first image if present
                if (importedRecipe.ImageData != null && importedRecipe.ImageData.Length > 0)
                {
                    try
                    {
                        var safeFileName = SanitizeFileName(Path.GetFileName(fileName) ?? "image");
                        var ext = Path.GetExtension(safeFileName);
                        var contentType = GetContentTypeFromExtension(ext);
                        await _recipes.AddImageAsync(userId, recipe.Id, new MemoryStream(importedRecipe.ImageData), safeFileName, contentType, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to attach image for imported recipe {RecipeId}", recipe.Id);
                    }
                }
            }
            if (!created.Any())
                throw new ApplicationException("No recipe data extracted.");
            return new ImportResult(true, null, created);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while importing AI-detected recipe {FileName}", fileName);
            return new ImportResult(false, ex.Message, created);
        }
        
    }

    // Hilfsmethode: Parsen einer Zutatenzeile in RecipeCreateIngredient.
    // Unterstützt: Ganzzahlen, Dezimalzahlen (',' oder '.'), Brüche ("1/2"), gemischte Zahlen ("1 1/2") und unicode‑Brüche ("½").
    private static RecipeCreateIngredient ParseIngredientLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new RecipeCreateIngredient(0m, null, string.Empty);

        var qty = line.Trim().TrimStart('*', '-', '•').Trim();

        // Unicode vulgar fractions map
        var vulgar = new Dictionary<char, decimal>
        {
            ['½'] = 0.5m,
            ['⅓'] = 1m / 3m,
            ['⅔'] = 2m / 3m,
            ['¼'] = 0.25m,
            ['¾'] = 0.75m,
            ['⅛'] = 0.125m
        };

        decimal amount = 0m;
        string? unit = null;
        string name = qty;

        // Mixed number: "1 1/2 ..." or "1-1/2 ..."
        var m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(\d+)[\s\-]+(\d+)\/(\d+)\s*(.*)$");
        if (m.Success)
        {
            var whole = int.Parse(m.Groups[1].Value);
            var num = int.Parse(m.Groups[2].Value);
            var den = int.Parse(m.Groups[3].Value);
            if (den != 0)
                amount = whole + (decimal)num / den;
            var rest = m.Groups[4].Value.Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Simple fraction: "1/2 ..."
        m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(\d+)\/(\d+)\s*(.*)$");
        if (m.Success)
        {
            var num = int.Parse(m.Groups[1].Value);
            var den = int.Parse(m.Groups[2].Value);
            if (den != 0)
                amount = (decimal)num / den;
            var rest = m.Groups[3].Value.Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Decimal or integer: "1.5 g ..." or "1,5 g ..." or "2 g"
        m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(\d+[.,]?\d*)\s*(.*)$");
        if (m.Success)
        {
            var numStr = m.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(numStr, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var val))
                amount = val;
            var rest = m.Groups[2].Value.Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Leading unicode vulgar fraction: "½ Zwiebel ..."
        if (qty.Length > 0 && vulgar.TryGetValue(qty[0], out var v))
        {
            amount = v;
            var rest = qty.Substring(1).Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // No amount detected — treat whole line as name
        return new RecipeCreateIngredient(0m, null, qty);
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
    private static string GetContentTypeFromExtension(string ext)
    {
        ext = ext?.ToLowerInvariant() ?? string.Empty;
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
    static void AssignUnitAndName(string rest, out string? parsedUnit, out string parsedName)
    {
        parsedUnit = null;
        parsedName = rest ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rest))
            return;

        var parts = rest.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            // Ein einzelnes Token: eher Bezeichnung als Einheit -> setzen als Name
            parsedName = parts[0];
            parsedUnit = null;
        }
        else
        {
            // Zwei Teile: erst Teil als Einheit, zweiter als Name
            parsedUnit = parts[0];
            parsedName = parts[1];
        }
    }
}
