namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// gits the hub release asset.
/// </summary>
/// <param name="Id">The id parameter.</param>
/// <param name="Name">The name parameter.</param>
/// <param name="DownloadUrl">The download url parameter.</param>
/// <returns>The result.</returns>
public sealed record GitHubReleaseAsset(long Id, string Name, string DownloadUrl);

/// <summary>
/// gits the hub release info.
/// </summary>
/// <param name="Id">The id parameter.</param>
/// <param name="TagName">The tag name parameter.</param>
/// <param name="Assets">The assets parameter.</param>
/// <returns>The result.</returns>
public sealed record GitHubReleaseInfo(long Id, string TagName, IReadOnlyList<GitHubReleaseAsset> Assets)
{
    /// <summary>
    /// Finds the zip asset.
    /// </summary>
    /// <returns>The result.</returns>
    public GitHubReleaseAsset? FindZipAsset()
    {
        return Assets
            .Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Name.Equals("release.zip", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
