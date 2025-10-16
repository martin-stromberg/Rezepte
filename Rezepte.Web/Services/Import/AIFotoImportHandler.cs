using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Entities;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Rezepte.Web.Services.Import;

public class GoogleQuotaClient
{
    private readonly string _serviceAccountJsonPath;
    private readonly HttpClient _httpClient;

    public GoogleQuotaClient(string serviceAccountJsonPath)
    {
        _serviceAccountJsonPath = serviceAccountJsonPath;
        _httpClient = new HttpClient();
    }

    public async Task<string> GetQuotaAsync(string serviceName, string projectId)
    {
        var credential = GoogleCredential
            .FromFile(_serviceAccountJsonPath)
            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

        var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string url = $"https://serviceusage.googleapis.com/v1beta1/projects/{projectId}/services/{serviceName}/consumerQuotaMetrics";
        var response = await _httpClient.GetAsync(url);
        var result = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return result;
    }
}
public class BaseAIImportHandler(IHttpContextAccessor httpContextAccessor, IAiUsageService aiUsage)
{
    protected IAiUsageService AiUsageService => aiUsage;
    public string UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userId ?? "unknown";
        }
    }
    protected string ServicecAcountFile
    {
        get
        {
            const string fileName = "google.application-credentials.json";
            // Programmdirectory (auch in Veröffentlichungen verlässlich)
            var jsonPath = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, fileName);

            // Nur die Umgebungsvariable setzen, wenn die Datei tatsächlich vorhanden ist
            if (File.Exists(jsonPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);
            }

            return jsonPath;
        }
    }

}
public class AIFotoImportHandler : BaseAIImportHandler, IImportHandler
{
    public AIFotoImportHandler(IRecipeService recipes,
        IOptionsMonitor<AIOptions> aioptions,
        IAiUsageService _aiUsage,
        IMemoryCache cache,
        ILogger<AIFotoImportHandler> logger,
        IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor, _aiUsage)
    {
        _recipes = recipes;
        this.aioptions = aioptions;
        _cache = cache;
        _logger = logger;
    }

    private bool IsSimulationModeActive
    {
        get => aioptions.CurrentValue.Simulate;
    }
    private bool IsCacheEnabled     {
        get => aioptions.CurrentValue.EnableCache;
    }
    private int CacheDurationHours
    {
        get => aioptions.CurrentValue.CacheDurationHours;
    }
    
    
    private struct AIRecipe
    {
        public string Title { get; set; }
        public List<string> Ingredients { get; set; }
        public string Instructions { get; set; }
        public byte[]? ImageData { get; set; }
    }

    private KeyValuePair<string, AIRecipe> lastRecipe;
    
    // Default cache duration when option missing/invalid
    private static readonly TimeSpan DefaultParsedImageCacheDuration = TimeSpan.FromDays(7);
    private readonly IRecipeService _recipes;
    private readonly IOptionsMonitor<AIOptions> aioptions;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIFotoImportHandler> _logger;



    /// <summary>
    /// Versucht zu erkennen, ob die gegebene Datei ein unterstütztes Bild ist und ob Dokumententext erkannt werden kann.
    /// Liest das Bild aus dem übergebenen <see cref="Stream"/> (nicht aus einer Datei).
    /// </summary>
    public async Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrEmpty(fileName)) return false;

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext)) return false;
        ext = ext.ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp" && ext != ".bmp") return false;

        if (!IsActive())
            return false;

        try
        {            
            byte[] imageBytes = await ReadImage(stream, ct).ConfigureAwait(false);

            // Compute image hash (independent of filename)
            var hash = ComputeSha256Hex(imageBytes);
            var cacheKey = $"AIFotoImport:{hash}";

            // Only consult cache if enabled in options
            if (IsCacheEnabled)
            {
                if (_cache.TryGetValue(cacheKey, out AIRecipe cachedRecipe))
                {
                    _logger.LogInformation("AIFotoImport cache hit for image hash {Hash}", hash);
                    lastRecipe = new KeyValuePair<string, AIRecipe>(fileName, cachedRecipe);
                    return true;
                }
            }
            else
            {
                _logger.LogDebug("AIFotoImport cache disabled by configuration.");
            }

            if (IsSimulationModeActive)
            {
                await Task.Delay(3000);
                var sim = CreateSimulationReceipt(imageBytes);

                if (IsCacheEnabled && CacheDurationHours > 0)
                {
                    var ttl = TimeSpan.FromHours(CacheDurationHours);
                    _cache.Set(cacheKey, sim, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
                    _logger.LogInformation("AIFotoImport cached simulation result for {Hours}h (hash={Hash})", CacheDurationHours, hash);
                }

                lastRecipe = new KeyValuePair<string, AIRecipe>(fileName, sim);
                return true;
            }

            //ToDo: KPi für Anzahl der API-Anfragen (versuche, erfolge) erfassen und auf Infoseite anzeigen
            Image image = await InitializeImage(stream, ct).ConfigureAwait(false);

            (bool flowControl, string recipeContent) = await ReadTextFromImage(image, ct);
            if (!flowControl || string.IsNullOrWhiteSpace(recipeContent))
              return false;

            AIRecipe extractedRecipe = ParseRecipe(recipeContent);
            extractedRecipe.ImageData = imageBytes;

            // Cache parsed result only if enabled and duration > 0
            if (IsCacheEnabled && CacheDurationHours > 0)
            {
                var ttl = TimeSpan.FromHours(CacheDurationHours);
                if (ttl <= TimeSpan.Zero)
                    ttl = DefaultParsedImageCacheDuration;

                _cache.Set(cacheKey, extractedRecipe, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });

                _logger.LogInformation("AIFotoImport cached parsed recipe for {Hours}h (hash={Hash})", ttl.TotalHours, hash);
            }

            lastRecipe = new KeyValuePair<string, AIRecipe>(fileName, extractedRecipe);

            return !string.IsNullOrWhiteSpace(lastRecipe.Value.Title) && lastRecipe.Value.Instructions.Any() && !string.IsNullOrWhiteSpace(lastRecipe.Value.Instructions);
        }
        catch
        {
            // Detection failed -> not handleable
            return false;
        }
    }

    private AIRecipe CreateSimulationReceipt(byte[] imageBytes)
    {
        return new AIRecipe()
        {
            Title = $"Simuliertes Rezepts {DateTime.Now.ToString()}",
            Instructions = "Die ist die Beschreibung eines simulierten Rezeptes.\r\nWenn Sie diesen Text in einer produktiven Systemumgebung lesen, dann kontaktieren Sie den Betreiber der Anwendung.",
            Ingredients = Enumerable.Range(1, 10).Select(i => $"{i} Stk. Zutat {i}").ToList(),
            ImageData = imageBytes
        };
    }

    private static async Task<byte[]> ReadImage(Stream stream, CancellationToken ct)
    {
        byte[] imageBytes;
        await using (var ms = new MemoryStream())
        {
            if (stream.CanSeek)
            {
                var originalPos = stream.Position;
                await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                stream.Position = originalPos;
            }
            else
            {
                await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
            }
            imageBytes = ms.ToArray();
        }

        return imageBytes;
    }

    private AIRecipe ParseRecipe(string recipeContent)
    {
        AIRecipe extractedRecipe = new AIRecipe();
        extractedRecipe.Title = ParseInformation(recipeContent, "Titel des Rezepts");
        extractedRecipe.Ingredients = ParseInformation(recipeContent, "Zutatenliste").Split("\r\n").Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.TrimStart(' ', '*')).ToList();
        extractedRecipe.Instructions = ParseInformation(recipeContent, "Zubereitungsschritte");
        return extractedRecipe;
    }

    private async Task<(bool flowControl, string recipeContent)> ReadTextFromImage(Image image, CancellationToken ct)
    {
        var client = ImageAnnotatorClient.Create();
        await AiUsageService.RecordRequestAsync(UserId, "Vision.Image.Requests", ct);
        var response = client.DetectDocumentText(image);
        var extractedText = response.Text;
        if (string.IsNullOrWhiteSpace(extractedText))
            return (flowControl: false, recipeContent: string.Empty);
        await AiUsageService.RecordRequestAsync(UserId, "Vision.Image.Success", ct);

        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Image.Requests", ct);
        var gemini = new GeminiClient(ServicecAcountFile);
        var resultContent = await gemini.ExtractRecipeAsync(extractedText);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Image.Success", ct);
        return (flowControl: true, recipeContent: resultContent);
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

    private static async Task<Image> InitializeImage(Stream stream, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        if (stream.CanSeek)
        {
            var originalPos = stream.Position;
            await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
            stream.Position = originalPos;
        }
        else
        {
            await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        }
        ms.Position = 0;
        var image = Image.FromStream(ms);
        return image;
    }

    private bool IsActive()
    {
        if (!File.Exists(ServicecAcountFile))
            return false;
        //var quotaClient = new GoogleQuotaClient(ServicecAcountFile);
        //string projectId = "rezepteverwaltung-475218";

        //// Vision API
        //string visionQuota = await quotaClient.GetQuotaAsync("vision.googleapis.com", projectId);
        //Console.WriteLine("Vision API Quota:");
        //Console.WriteLine(visionQuota);

        //// Gemini / Generative Language API
        //string geminiQuota = await quotaClient.GetQuotaAsync("generativelanguage.googleapis.com", projectId);
        //Console.WriteLine("Gemini API Quota:");
        //Console.WriteLine(geminiQuota);
        return true;
    }

    public static string ExtractTextFromResponse(string jsonResponse)
    {
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;
        var text = root
            .GetProperty("responses")[0]
            .GetProperty("fullTextAnnotation")
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    public static async Task<string> SendVisionRequestAsync(string base64Image, string apiKey)
    {
        using var httpClient = new HttpClient();

        var requestBody = new
        {
            requests = new[]
            {
            new
            {
                image = new { content = base64Image },
                features = new[] { new { type = "DOCUMENT_TEXT_DETECTION" } }
            }
        }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(
            $"https://vision.googleapis.com/v1/images:annotate?key={apiKey}",
            content
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public static string LoadImageAsBase64(string imagePath)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        return Convert.ToBase64String(imageBytes);
    }


    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (lastRecipe.Key != fileName)
            throw new InvalidOperationException("CanHandleAsync must be called and return true before HandleAsync.");
        var importedRecipe = lastRecipe.Value;
        if (string.IsNullOrWhiteSpace(importedRecipe.Title) && (importedRecipe.Ingredients == null || importedRecipe.Ingredients.Count == 0) && string.IsNullOrWhiteSpace(importedRecipe.Instructions))
            throw new InvalidOperationException("No parsed recipe available. Call CanHandleAsync first.");

        var created = new List<string>();

        try
        {
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

    // Lokale Hilfsfunktion zum Aufteilen von "unit name" (unit optional)
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

    private static string ComputeSha256Hex(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}