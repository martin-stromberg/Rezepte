using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using static Rezepte.Web.Services.Import.GeminiClient;

namespace Rezepte.Web.Services.Import;

public abstract class BaseAIImportHandler(
    IOptionsMonitor<AIOptions> aioptions, 
    IHttpContextAccessor httpContextAccessor, 
    IAiUsageService aiUsage, 
    IRecipeService recipeService, 
    IGoogleServiceAccountProvider serviceAccountProvider,
    ISettingsService settingsService,
    ILogger logger)
{
    private KeyValuePair<string, AIRecipe[]> _lastRecipes;
    private StreamReader lastReader = null;
    protected abstract bool IsTextMode();
    protected readonly IRecipeService recipeService = recipeService;
    protected readonly ILogger _logger = logger;
    protected IAiUsageService AiUsageService => aiUsage;
    protected readonly IGoogleServiceAccountProvider _serviceAccountProvider = serviceAccountProvider;

    protected string ServicecAcountFile => _serviceAccountProvider.GetFilePath();
    public string UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userId ?? "unknown";
        }
    }

    protected virtual async Task<bool> IsActiveAsync()
    {
        if (!_serviceAccountProvider.Exists())
            return false;
        if (!await SettingsService.GetGlobalAiEnabledAsync())
            return false;
        if (!await SettingsService.GetUserAiEnabledAsync(UserId))
            return false;
        return true;
    }

    private bool IsSimulationModeActive
    {
        get => aioptions.CurrentValue.Simulate;
    }
    public ISettingsService SettingsService { get; } = settingsService;

    protected virtual AIRecipe CreateSimulationReceipt(byte[] imageBytes)
    {
        return new AIRecipe()
        {
            Title = $"Simuliertes Rezepts {DateTime.Now.ToString()}",
            Instructions = "Die ist die Beschreibung eines simulierten Rezeptes.\r\nWenn Sie diesen Text in einer produktiven Systemumgebung lesen, dann kontaktieren Sie den Betreiber der Anwendung.",
            Ingredients = Enumerable.Range(1, 10).Select(i => $"{i} Stk. Zutat {i}").ToList(),
            ImageData = imageBytes
        };
    }

    public async Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (lastReader is not null)
            lastReader.Dispose();

        if (!await IsActiveAsync())
            return false;

        if (IsTextMode())
        {
            if (IsSimulationModeActive)
            {
                await Task.Delay(3000); // Simulate some delay
                _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, new AIRecipe[] { CreateSimulationReceipt(new byte[0]) });
                return true;
            }

            lastReader = new StreamReader(stream);
            try
            {
                var responseContent = lastReader.ReadToEnd();
                _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, await ReadRecipeCollection(fileName, stream, responseContent, ct));
                return _lastRecipes.Value.Any();
            }
            catch
            {
                return false;
            }
        }
        else
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrEmpty(fileName)) return false;

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext)) return false;
            ext = ext.ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp" && ext != ".bmp") return false;

            if (IsSimulationModeActive)
                _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, new AIRecipe[] { CreateSimulationReceipt(new byte[0]) });
            else
                _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, await ReadRecipeCollection(fileName, stream, "", ct));
            return _lastRecipes.Value.Any();
        }
    }
    protected abstract Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct);
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
                        DurationMinutes: importedRecipe.PreparationTimeInMinutes,
                        RequiresOvernightRest: false,
                        Ingredients: stepIngredients)
                };

                var (ok, error, recipe) = await recipeService.CreateAsync(userId, targetCookbookId, importedRecipe.Title ?? "Importiertes Fotorezept", null, null, importedRecipe.Portions, steps, ct).ConfigureAwait(false);
                if (!ok || recipe == null)
                {
                    logger.LogWarning("Failed to create recipe from AI import: {Title} - {Error}", importedRecipe.Title, error);
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
                        await recipeService.AddImageAsync(userId, recipe.Id, new MemoryStream(importedRecipe.ImageData), safeFileName, contentType, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to attach image for imported recipe {RecipeId}", recipe.Id);
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
            logger.LogError(ex, "Error while importing AI-detected recipe {FileName}", fileName);
            return new ImportResult(false, ex.Message, created);
        }

    }
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
