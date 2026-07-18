namespace Rezepte.Web.Services.Import.Plugins;

public interface IGitHubReleaseClient
{
    Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default);
    Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default);
}
