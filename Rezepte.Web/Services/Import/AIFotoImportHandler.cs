using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Entities;
using System.Formats.Asn1;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static Rezepte.Web.Services.Import.GeminiClient;

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
public class AIFotoImportHandler : BaseAIImportHandler, IImportHandler
{
    public AIFotoImportHandler(IRecipeService recipes,
        IOptionsMonitor<AIOptions> aioptions,
        IAiUsageService _aiUsage,
        IMemoryCache cache,
        ILogger<AIFotoImportHandler> logger,
        IHttpContextAccessor httpContextAccessor) : base(aioptions, httpContextAccessor, _aiUsage, recipes, logger)
    {
        this.aioptions = aioptions;
        _cache = cache;
    }

    
    private bool IsCacheEnabled     {
        get => aioptions.CurrentValue.EnableCache;
    }
    private int CacheDurationHours
    {
        get => aioptions.CurrentValue.CacheDurationHours;
    }
    protected override bool IsTextMode()
    {
        return false;
    }
       
    // Default cache duration when option missing/invalid
    private static readonly TimeSpan DefaultParsedImageCacheDuration = TimeSpan.FromDays(7);
    private readonly IOptionsMonitor<AIOptions> aioptions;
    private readonly IMemoryCache _cache;

    protected override async Task<GeminiClient.AIRecipe[]> ReadRecipeCollection(string fileName, Stream stream, string responseContent, CancellationToken ct)
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
        var client = ImageAnnotatorClient.Create();
        await AiUsageService.RecordRequestAsync(UserId, "Vision.Image.Requests", ct);
        var response = client.DetectDocumentText(image);
        var extractedText = response.Text;
        if (string.IsNullOrWhiteSpace(extractedText))
            return (flowControl: false, recipes: new AIRecipe[0]);
        await AiUsageService.RecordRequestAsync(UserId, "Vision.Image.Success", ct);

        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Image.Requests", ct);
        var gemini = new GeminiClient(ServicecAcountFile);
        var resultContent = await gemini.ExtractRecipeAsync(extractedText);
        await AiUsageService.RecordRequestAsync(UserId, "Gemini.Image.Success", ct);
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