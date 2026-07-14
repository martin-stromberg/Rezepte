using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Configuration;
using static Rezepte.Web.Services.Import.GeminiClient;
using System.Text.RegularExpressions;

namespace Rezepte.Web.Services.Import;
public abstract class BaseAIImportHandler(
    IOptionsMonitor<AIOptions> aioptions, 
    IAiUsageService aiUsage, 
    IRecipeService recipeService, 
    IGeminiClient geminiClient,
    ISettingsService settingsService,
    ILogger logger): BaseImportHandler, IInteractiveImportHandler
{
    private KeyValuePair<string, AIRecipe[]> _lastRecipes;
    private StreamReader lastReader = null;
    private string _responseContent = string.Empty;
    protected abstract bool IsTextMode();
    protected readonly IRecipeService recipeService = recipeService;
    protected readonly ILogger _logger = logger;
    protected IAiUsageService AiUsageService => aiUsage;
    private readonly IGeminiClient geminiClient = geminiClient;

    protected IGeminiClient CreateGeminiClient()
    {
        return geminiClient;
    }

    protected virtual async Task<bool> IsActiveAsync()
    {
        if (!geminiClient.HasServiceAccount())
            return false;
        if (!await SettingsService.GetGlobalAiEnabledAsync())
            return false;
        if (!await SettingsService.GetUserAiEnabledAsync(UserId))
            return false;
        return true;
    }
    public static bool LooksLikeHtmlDocument(string input)
    {
        return input.Contains("<html") && input.Contains("<body");
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
        _responseContent = "";
        _lastRecipes = new KeyValuePair<string, AIRecipe[]>();
        if (IsTextMode())
        {
            if (IsSimulationModeActive)
                return true;

            lastReader = new StreamReader(stream);
            try
            {
                _responseContent = lastReader.ReadToEnd();
                return LooksLikeHtmlDocument(_responseContent);
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
            return true;
        }
    }
    protected abstract Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct);
    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (IsSimulationModeActive)
        {
            await Task.Delay(3000);
            _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, new AIRecipe[] { CreateSimulationReceipt(new byte[0]) });
        } 
        else
            _lastRecipes = new KeyValuePair<string, AIRecipe[]>(fileName, await ReadRecipeCollection(fileName, stream, _responseContent, ct));
        if (_lastRecipes.Key != fileName)
            throw new InvalidOperationException("No receipe information was extracted.");
        var importedRecipes = _lastRecipes.Value;
        try
        {
            var neutralRecipes = importedRecipes
                .Where(importedRecipe => !string.IsNullOrWhiteSpace(importedRecipe.Title)
                    || importedRecipe.Ingredients is { Count: > 0 }
                    || !string.IsNullOrWhiteSpace(importedRecipe.Instructions))
                .Select(importedRecipe => ToImportedRecipe(importedRecipe, uri, fileName))
                .ToList();

            if (!neutralRecipes.Any())
                throw new ApplicationException("No recipe data extracted.");

            return new ImportResult(true, null, new List<string>(), neutralRecipes);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while importing AI-detected recipe {FileName}", fileName);

            // create a user-friendly, localized short message while the full exception remains in the logs
            var friendly = ImportExceptionHelper.BeautifyExceptionMessage(ex);
            return new ImportResult(false, friendly, new List<string>());
        }

    }
    private ImportedRecipe ToImportedRecipe(AIRecipe importedRecipe, string? uri, string fileName)
    {
        var ingredients = (importedRecipe.Ingredients ?? new List<string>())
            .Select(line =>
            {
                var ingredient = ParseIngredientLine(line);
                return new ImportedIngredient
                {
                    Quantity = $"{ingredient.Amount} {ingredient.Unit}".Trim(),
                    Name = ingredient.Name
                };
            })
            .ToList();

        var images = new List<ImportedImage>();
        if (importedRecipe.ImageData is { Length: > 0 })
        {
            var safeFileName = SanitizeFileName(Path.GetFileName(fileName) ?? "image");
            images.Add(new ImportedImage
            {
                Data = importedRecipe.ImageData,
                FileName = safeFileName,
                ContentType = GetContentTypeFromExtension(Path.GetExtension(safeFileName))
            });
        }

        return new ImportedRecipe
        {
            Title = importedRecipe.Title ?? "Importiertes Fotorezept",
            SourceUri = uri,
            Portions = importedRecipe.Portions,
            WorkTimeMinutes = importedRecipe.PreparationTimeInMinutes,
            Ingredients = ingredients,
            Steps =
            [
                new ImportedRecipeStep
                {
                    Text = importedRecipe.Instructions ?? string.Empty
                }
            ],
            Images = images
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
    

    public async Task<ImportResult> HandleInteractiveAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, IImportInteraction interaction, CancellationToken ct = default)
    {
        if (!await HandleConfirmation(interaction))
            return new ImportResult(false, "Import cancelled by user.", new List<string>());

        await interaction.ReportStatusAsync("Importiere Rezepte...");
        return await HandleAsync(stream, fileName, uri, targetCookbookId, userId, ct);
    }

    protected virtual async Task<bool> HandleConfirmation(IImportInteraction interaction)
    {
        return await interaction.AskForConfirmationAsync("Weitermachen?");
    }

    private string BeautifyExceptionMessage(Exception ex)
    {
        // unwrap to the most relevant exception
        while (ex.InnerException is not null)
            ex = ex.InnerException;

        var raw = ex.Message ?? ex.ToString();

        // try to extract a detailed 'Detail="...""' if present (e.g. Google/Rpc style)
        var detailMatch = Regex.Match(raw, "Detail\\s*=\\s*\"(?<d>.*?)\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        string detail = detailMatch.Success ? detailMatch.Groups["d"].Value : string.Empty;

        if (string.IsNullOrEmpty(detail))
        {
            // alternate pattern: detail: ... or \"detail\": "...
            var altMatch = Regex.Match(raw, "(\"detail\"\\s*[:=]\\s*\"(?<d>.*?)\")|detail\\s*[:=]\\s*(?<d>https?://[^\\s]+|.+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (altMatch.Success) detail = altMatch.Groups["d"].Value;
        }

        // Truncate long details and remove sensitive data (URLs left as-is for operator)
        string Shorten(string s, int max = 300) => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…");

        // Friendly, localized message
        if (!string.IsNullOrEmpty(detail))
        {
            // special-case hints
            if (detail.IndexOf("billing", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Zugriff verweigert / Abrechnung erforderlich: {Shorten(detail)}";
            }
            if (detail.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0 || detail.IndexOf("permissiondenied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Zugriffsfehler: {Shorten(detail)}";
            }

            return Shorten(detail);
        }

        // fallback: try to provide a concise summary from the raw message
        var firstLine = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? raw;
        return Shorten(firstLine);
    }
}
