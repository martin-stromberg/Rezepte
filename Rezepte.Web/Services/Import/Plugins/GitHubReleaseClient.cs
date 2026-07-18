using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class GitHubReleaseClient(HttpClient httpClient, IOptions<PluginUpdateOptions> options) : IGitHubReleaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PluginUpdateOptions _options = options.Value;

    public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildApiUri($"/repos/{repository.Owner}/{repository.Repository}/releases/latest"));
        ApplyHeaders(request, personalAccessToken);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
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
            var url = asset.TryGetProperty("browser_download_url", out var browserDownloadUrl)
                ? browserDownloadUrl.GetString() ?? string.Empty
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(url))
            {
                assets.Add(new GitHubReleaseAsset(assetId, name, url));
            }
        }

        return new GitHubReleaseInfo(id, tagName, assets);
    }

    public async Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
        ApplyHeaders(request, personalAccessToken);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var output = File.Create(targetPath);
        await input.CopyToAsync(output, ct).ConfigureAwait(false);
    }

    private Uri BuildApiUri(string path)
    {
        var baseUri = new Uri(_options.GitHubApiBaseUrl.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/'));
    }

    private void ApplyHeaders(HttpRequestMessage request, string? personalAccessToken)
    {
        request.Headers.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(_options.UserAgent) ? "Rezepte.PluginUpdater" : _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(personalAccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
        }
    }
}
