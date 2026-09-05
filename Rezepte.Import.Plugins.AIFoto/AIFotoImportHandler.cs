using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Rezepte.Import.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Import.Plugins.AIFoto;

/// <summary>
/// Import handler that extracts recipe text from photos using Google Vision and Gemini.
/// </summary>
public class AIFotoImportHandler : BaseAIImportHandler, IImportHandler
{
    private static readonly TimeSpan DefaultParsedImageCacheDuration = TimeSpan.FromDays(7);
    private readonly IOptionsMonitor<AIOptions> _aiOptions;
    private readonly IMemoryCache _cache;
    private readonly IGoogleCredentialsProvider _credentialsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFotoImportHandler"/> class.
    /// </summary>
    /// <param name="recipes">Recipe service used to persist imported recipes.</param>
    /// <param name="aioptions">AI options monitor.</param>
    /// <param name="aiUsage">AI usage accounting service.</param>
    /// <param name="cache">Memory cache for parsed image results.</param>
    /// <param name="geminiClient">Gemini client for recipe extraction.</param>
    /// <param name="credentialsProvider">Provider for Google Vision credentials.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="settings">Settings service.</param>
    public AIFotoImportHandler(
        IRecipeService recipes,
        IOptionsMonitor<AIOptions> aioptions,
        IAiUsageService aiUsage,
        IMemoryCache cache,
        IGeminiClient geminiClient,
        IGoogleCredentialsProvider credentialsProvider,
        ILogger<AIFotoImportHandler> logger,
        ISettingsService settings)
        : base(aioptions, aiUsage, recipes, geminiClient, settings, logger)
    {
        _aiOptions = aioptions;
        _cache = cache;
        _credentialsProvider = credentialsProvider;
    }

    /// <summary>
    /// Determines whether the handler is active for the current user and configuration.
    /// </summary>
    /// <returns><c>true</c> when the handler can be used; otherwise <c>false</c>.</returns>
    protected override async Task<bool> IsActiveAsync()
    {
        if (!await base.IsActiveAsync()) return false;

        var diagnostics = _credentialsProvider.GetDiagnostics();
        if (!diagnostics.ServiceAccountFileExists)
        {
            if (string.IsNullOrWhiteSpace(diagnostics.ServiceAccountFilePath))
            {
                LogInactive("Google Vision service account path is not configured");
            }
            else
            {
                _logger.LogWarning(
                    "{HandlerName} inactive for user {UserId}: Google Vision service account file was not found at {ServiceAccountFilePath}; source: {Source}",
                    GetType().Name,
                    UserId,
                    diagnostics.ServiceAccountFilePath,
                    diagnostics.ServiceAccountSource);
            }
            return false;
        }

        if (!HasGeminiAuthentication()) return false;
        if (!await SettingsService.GetGlobalGoogleVisionEnabledAsync())
        {
            LogInactive("global Google Vision is disabled");
            return false;
        }
        if (!await SettingsService.GetUserGoogleVisionEnabledAsync(UserId))
        {
            LogInactive("user Google Vision is disabled");
            return false;
        }
        if (!await SettingsService.GetGlobalGeminiEnabledAsync())
        {
            LogInactive("global Gemini is disabled");
            return false;
        }
        if (!await SettingsService.GetUserGeminiEnabledAsync(UserId))
        {
            LogInactive("user Gemini is disabled");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the handler works with text input.
    /// </summary>
    /// <returns><c>false</c> because this handler processes image data.</returns>
    protected override bool IsTextMode() => false;

    /// <summary>
    /// Asks the user to confirm the AI analysis of the provided image.
    /// </summary>
    /// <param name="interaction">Interaction surface used to ask for confirmation.</param>
    /// <returns><c>true</c> when the user confirmed; otherwise <c>false</c>.</returns>
    protected override Task<bool> HandleConfirmation(IImportInteraction interaction) =>
        interaction.AskForConfirmationAsync("Die Texte der angegebenen Bilddatei werden mittels KI extrahiert und analysiert. Fortfahren?");

    /// <summary>
    /// Reads recipes from the provided image using Google Vision and Gemini.
    /// </summary>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="stream">Stream containing the image data.</param>
    /// <param name="responseContent">Unused for image input.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An array of extracted recipes.</returns>
    protected override async Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
    {
        var imageBytes = await ReadImage(stream, ct).ConfigureAwait(false);
        var cacheKey = $"AIFotoImport:{Convert.ToHexString(SHA256.HashData(imageBytes))}";
        if (_aiOptions.CurrentValue.EnableCache && _cache.TryGetValue(cacheKey, out AIRecipe[]? cached)) return cached ?? [];

        var image = await InitializeImage(stream, ct).ConfigureAwait(false);
        var allowedVision = await AiUsageService.TryRecordRequestAsync(UserId, "Vision.Image", ct);
        if (!allowedVision) return [];
        var extractedText = CreateVisionClient().DetectDocumentText(image).Text;
        if (string.IsNullOrWhiteSpace(extractedText)) return [];
        await AiUsageService.RecordRequestAsync(UserId, "Vision.Image", AiRequestLogType.Success, ct);
        if (!await AiUsageService.TryRecordRequestAsync(UserId, "Gemini.Image", ct)) return [];
        var recipes = await CreateGeminiClient().ExtractRecipeAsync(extractedText, ct);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Image", AiRequestLogType.Success, ct);
        foreach (var recipe in recipes.Where(r => r.ImageData is null or { Length: 0 })) recipe.ImageData = imageBytes;

        if (_aiOptions.CurrentValue.EnableCache && _aiOptions.CurrentValue.CacheDurationHours > 0)
            _cache.Set(cacheKey, recipes, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_aiOptions.CurrentValue.CacheDurationHours) });
        return recipes;
    }

    private static async Task<byte[]> ReadImage(Stream stream, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        var position = stream.CanSeek ? stream.Position : 0;
        await stream.CopyToAsync(ms, ct);
        if (stream.CanSeek) stream.Position = position;
        return ms.ToArray();
    }

    private static async Task<Image> InitializeImage(Stream stream, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        var position = stream.CanSeek ? stream.Position : 0;
        await stream.CopyToAsync(ms, ct);
        if (stream.CanSeek) stream.Position = position;
        ms.Position = 0;
        return Image.FromStream(ms);
    }

    private ImageAnnotatorClient CreateVisionClient()
    {
        var diagnostics = _credentialsProvider.GetDiagnostics();
        _logger.LogInformation(
            "Initializing Google Vision client with service account file from {Source} at {ServiceAccountFilePath}",
            diagnostics.ServiceAccountSource,
            diagnostics.ServiceAccountFilePath);

        // GoogleCredential.FromFile(string) is obsolete (potential security risk loading an
        // unvalidated credential configuration); CredentialFactory.FromFile<T> loads a
        // specifically-typed credential instead, converted back via ToGoogleCredential() for
        // CreateScoped.
        var credential = CredentialFactory.FromFile<ServiceAccountCredential>(diagnostics.ServiceAccountFilePath)
            .ToGoogleCredential()
            .CreateScoped(ImageAnnotatorClient.DefaultScopes);

        return new ImageAnnotatorClientBuilder
        {
            GoogleCredential = credential
        }.Build();
    }
}
