using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// gits the hub release client.
/// </summary>
/// <param name="httpClient">The http client parameter.</param>
/// <param name="options">The options parameter.</param>
/// <returns>The result.</returns>
public sealed class GitHubReleaseClient(HttpClient httpClient, IOptions<PluginUpdateOptions> options) : IGitHubReleaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PluginUpdateOptions _options = options.Value;

    /// <summary>
    /// Gets the latest release async.
    /// </summary>
    /// <param name="repository">The repository parameter.</param>
    /// <param name="personalAccessToken">The personal access token parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildApiUri($"/repos/{repository.Owner}/{repository.Repository}/releases/latest"));
        ApplyHeaders(request, personalAccessToken);
        using var response = await SendWithRetryAsync(request, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var root = doc.RootElement;
        var id = root.GetProperty("id").GetInt64();
        var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
        var assets = new List<GitHubReleaseAsset>();
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var assetId = asset.GetProperty("id").GetInt64();
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            var url = asset.TryGetProperty("url", out var apiUrl)
                ? apiUrl.GetString() ?? string.Empty
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(url))
            {
                assets.Add(new GitHubReleaseAsset(assetId, name, url));
            }
        }

        return new GitHubReleaseInfo(id, tagName, assets);
    }

    /// <summary>
    /// downloads the asset async.
    /// </summary>
    /// <param name="asset">The asset parameter.</param>
    /// <param name="targetPath">The target path parameter.</param>
    /// <param name="personalAccessToken">The personal access token parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
        ApplyHeaders(request, personalAccessToken, acceptOctetStream: true);
        using var response = await SendWithRetryAsync(request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var output = File.Create(targetPath);
        await input.CopyToAsync(output, ct).ConfigureAwait(false);
    }

    private Uri BuildApiUri(string path)
    {
        var baseUri = new Uri(_options.GitHubApiBaseUrl.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/'));
    }

    private void ApplyHeaders(HttpRequestMessage request, string? personalAccessToken, bool acceptOctetStream = false)
    {
        request.Headers.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(_options.UserAgent) ? "Rezepte.PluginUpdater" : _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptOctetStream ? "application/octet-stream" : "application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(personalAccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!IsRateLimited(response, out var retryAfter))
        {
            return response;
        }

        response.Dispose();
        if (retryAfter.HasValue && retryAfter.Value > TimeSpan.Zero)
        {
            await Task.Delay(retryAfter.Value > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : retryAfter.Value, ct).ConfigureAwait(false);
        }

        using var retryRequest = await CloneRequestAsync(request, ct).ConfigureAwait(false);
        var retryResponse = await httpClient.SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (IsRateLimited(retryResponse, out retryAfter))
        {
            retryResponse.Dispose();
            throw new GitHubRateLimitException("GitHub rate limit reached.", retryAfter);
        }

        return retryResponse;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var buffer = await request.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            clone.Content = new ByteArrayContent(buffer);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (IsRateLimited(response, out var retryAfter))
        {
            throw new GitHubRateLimitException("GitHub rate limit reached.", retryAfter);
        }

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(message) ? $"GitHub request failed with status {(int)response.StatusCode}." : message,
            null,
            response.StatusCode);
    }

    private static bool IsRateLimited(HttpResponseMessage response, out TimeSpan? retryAfter)
    {
        retryAfter = response.Headers.RetryAfter?.Delta;
        if (response.Headers.RetryAfter?.Date is { } retryDate)
        {
            retryAfter = retryDate - DateTimeOffset.UtcNow;
            if (retryAfter < TimeSpan.Zero)
            {
                retryAfter = TimeSpan.Zero;
            }
        }

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        if (response.StatusCode != System.Net.HttpStatusCode.Forbidden)
        {
            return false;
        }

        return response.Headers.TryGetValues("X-RateLimit-Remaining", out var values)
            && values.Any(v => string.Equals(v, "0", StringComparison.Ordinal));
    }
}
