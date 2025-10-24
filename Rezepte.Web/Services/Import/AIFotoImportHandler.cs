using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Entities;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static Rezepte.Web.Services.Import.GeminiClient;

namespace Rezepte.Web.Services.Import;
/// <summary>
/// Handles the import of AI-generated recipes from image files, leveraging AI services for text extraction and
/// processing.
/// </summary>
/// <remarks>This class is responsible for processing image files to extract recipe data using AI-based text
/// recognition.  It supports caching of parsed results to improve performance and reduce redundant processing for
/// identical images. The caching behavior is configurable via <see cref="AIOptions"/>.</remarks>
public class AIFotoImportHandler : BaseAIImportHandler, IImportHandler
{
    /// <summary>
    /// Handles the import of photos using AI-based processing and integrates with various services such as caching,
    /// logging, and external providers.
    /// </summary>
    /// <param name="recipes">The service used to manage and retrieve recipe-related data.</param>
    /// <param name="aioptions">The options monitor providing configuration settings for AI operations.</param>
    /// <param name="_aiUsage">The service used to track and manage AI usage metrics.</param>
    /// <param name="cache">The memory cache instance used to store temporary data for performance optimization.</param>
    /// <param name="logger">The logger instance used for logging diagnostic and operational messages.</param>
    /// <param name="googleServiceAccountProvider">The provider for Google service account credentials, used for accessing Google APIs.</param>
    /// <param name="settings">The service used to manage application settings.</param>
    /// <param name="httpContextAccessor">The accessor for the current HTTP context, used to retrieve request-specific information.</param>
    public AIFotoImportHandler(IRecipeService recipes,
        IOptionsMonitor<AIOptions> aioptions,
        IAiUsageService _aiUsage,
        IMemoryCache cache,
        IGeminiClient geminiClient,
        ILogger<AIFotoImportHandler> logger,
        ISettingsService settings) : base(aioptions, _aiUsage, recipes, geminiClient, settings,logger)
    {
        this.aioptions = aioptions;
        _cache = cache;
    }
    protected override async Task<bool> IsActiveAsync()
    {
        if (!await base.IsActiveAsync())
            return false;

        var globalGoogleVisionEnabled = await SettingsService.GetGlobalGoogleVisionEnabledAsync();
        if (!globalGoogleVisionEnabled)
            return false;
        var userGoogleVisionEnabled = await SettingsService.GetUserGoogleVisionEnabledAsync(UserId);
        if (!userGoogleVisionEnabled)
            return false;

        var globalGeminiEnabled = await SettingsService.GetGlobalGeminiEnabledAsync();
        if (!globalGeminiEnabled)
            return false;
        var userGeminiEnabled = await SettingsService.GetUserGeminiEnabledAsync(UserId);
        if (!userGeminiEnabled)
            return false;
        return true;
    }
    
    private bool IsCacheEnabled     {
        get => aioptions.CurrentValue.EnableCache;
    }
    private int CacheDurationHours
    {
        get => aioptions.CurrentValue.CacheDurationHours;
    }
    /// <summary>
    /// Determines whether the current mode is text-based.
    /// </summary>
    /// <returns><see langword="true"/> if the current mode is text-based; otherwise, <see langword="false"/>.</returns>
    protected override bool IsTextMode()
    {
        return false;
    }
    protected override async Task<bool> HandleConfirmation(IImportInteraction interaction)
    {
        return await interaction.AskForConfirmationAsync("Die Texte der angegebenen Bilddatei werden mittels KI extrahiert und analysiert. Fortfahren?");
    }
       
    // Default cache duration when option missing/invalid
    private static readonly TimeSpan DefaultParsedImageCacheDuration = TimeSpan.FromDays(7);
    private readonly IOptionsMonitor<AIOptions> aioptions;
    private readonly IMemoryCache _cache;
    /// <summary>
    /// Reads and processes a collection of AI-generated recipes from an image stream.
    /// </summary>
    /// <remarks>This method processes the provided image stream to extract AI-generated recipes. If caching
    /// is enabled, the method attempts to retrieve the recipes from the cache using a hash of the image data. If the
    /// recipes are not cached, the image is processed to extract the recipes, and the result is cached for future use
    /// based on the configured cache duration.</remarks>
    /// <param name="fileName">The name of the file being processed. Used for logging or identification purposes.</param>
    /// <param name="stream">The input stream containing the image data to be processed.</param>
    /// <param name="responseContent">The response content associated with the operation, used for additional context.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An array of <see cref="GeminiClient.AIRecipe"/> objects extracted from the image. Returns an empty array if no
    /// recipes are found.</returns>
    protected override async Task<AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
    {
        byte[] imageBytes = await ReadImage(stream, ct).ConfigureAwait(false);
        var hash = ComputeSha256Hex(imageBytes);
        var cacheKey = $"AIFotoImport:{hash}";
        if (IsCacheEnabled)
        {
            if (_cache.TryGetValue(cacheKey, out AIRecipe[] cachedRecipe))
            {
                _logger.LogInformation("AIFotoImport cache hit for image hash {Hash}", hash);
                return cachedRecipe;
            }
        }
        else
        {
            _logger.LogDebug("AIFotoImport cache disabled by configuration.");
        }

        Image image = await InitializeImage(stream, ct).ConfigureAwait(false);

        (bool flowControl, AIRecipe[] extractedRecipe) = await ReadTextFromImage(image, ct);
        if (!flowControl || !extractedRecipe.Any())
            return new AIRecipe[0];

        foreach (var recipe in extractedRecipe)
        {
            if (recipe.ImageData == null || recipe.ImageData.Length == 0)
                recipe.ImageData = imageBytes;
        }

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
        return extractedRecipe;
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
        
    private async Task<(bool flowControl, AIRecipe[] recipes)> ReadTextFromImage(Image image, CancellationToken ct)
    {
        // Check global limits before making the Vision request
        var allowedVision = await AiUsageService.TryRecordRequestAsync(UserId, "Vision.Image", ct);
        if (!allowedVision)
        {
            _logger.LogWarning("Vision request blocked due to AI limits for user {UserId}", UserId);
            return (flowControl: false, recipes: new AIRecipe[0]);
        }

        var client = ImageAnnotatorClient.Create();
        var response = client.DetectDocumentText(image);
        var extractedText = response.Text;
        if (string.IsNullOrWhiteSpace(extractedText))
            return (flowControl: false, recipes: new AIRecipe[0]);

        // record success marker (type = Success)
        await AiUsageService.RecordRequestAsync(UserId, "Vision.Image", AiRequestLogType.Success, ct);

        // Check and record Gemini request
        var allowedGemini = await AiUsageService.TryRecordRequestAsync(UserId, "Gemini.Image", ct);
        if (!allowedGemini)
        {
            _logger.LogWarning("Gemini request blocked due to AI limits for user {UserId}", UserId);
            return (flowControl: false, recipes: new AIRecipe[0]);
        }

        var gemini = CreateGeminiClient();
        var resultContent = await gemini.ExtractRecipeAsync(extractedText);
        // record gemini success
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Image", AiRequestLogType.Success, ct);
        return (flowControl: true, recipes: resultContent);
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