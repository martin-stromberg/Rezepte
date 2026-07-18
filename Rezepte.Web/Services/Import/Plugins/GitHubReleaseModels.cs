namespace Rezepte.Web.Services.Import.Plugins;

public sealed record GitHubReleaseAsset(long Id, string Name, string DownloadUrl);

public sealed record GitHubReleaseInfo(long Id, string TagName, IReadOnlyList<GitHubReleaseAsset> Assets)
{
    public GitHubReleaseAsset? FindZipAsset()
    {
        return Assets
            .Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Name.Equals("release.zip", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
