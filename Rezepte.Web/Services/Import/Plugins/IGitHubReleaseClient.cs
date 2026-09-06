namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the igit hub release client interface.
/// </summary>
public interface IGitHubReleaseClient
{
    /// <summary>
    /// Gets the latest release async.
    /// </summary>
    /// <param name="repository">The repository parameter.</param>
    /// <param name="personalAccessToken">The personal access token parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default);
    /// <summary>
    /// downloads the asset async.
    /// </summary>
    /// <param name="asset">The asset parameter.</param>
    /// <param name="targetPath">The target path parameter.</param>
    /// <param name="personalAccessToken">The personal access token parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default);
}
