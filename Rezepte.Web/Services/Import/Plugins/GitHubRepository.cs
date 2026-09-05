namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// gits the hub repository.
/// </summary>
/// <param name="Owner">The owner parameter.</param>
/// <param name="Repository">The repository parameter.</param>
/// <param name="CanonicalUrl">The canonical url parameter.</param>
/// <returns>The result.</returns>
public sealed record GitHubRepository(string Owner, string Repository, string CanonicalUrl)
{
    /// <summary>
    /// Parses the value.
    /// </summary>
    /// <param name="repositoryUrl">The repository url parameter.</param>
    /// <returns>The result.</returns>
    public static GitHubRepository Parse(string repositoryUrl)
    {
        if (!TryParse(repositoryUrl, out var repository))
        {
            throw new ArgumentException("Nur GitHub-Repository-URLs sind zulässig.", nameof(repositoryUrl));
        }

        return repository;
    }

    /// <summary>
    /// Tries to parse.
    /// </summary>
    /// <param name="repositoryUrl">The repository url parameter.</param>
    /// <param name="repository">The repository parameter.</param>
    /// <returns>The result.</returns>
    public static bool TryParse(string repositoryUrl, out GitHubRepository repository)
    {
        repository = default!;
        if (!Uri.TryCreate(repositoryUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = uri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return false;
        }

        var owner = parts[0];
        var repo = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? parts[1][..^4]
            : parts[1];
        if (owner.Any(IsInvalidSegmentChar) || repo.Any(IsInvalidSegmentChar))
        {
            return false;
        }

        repository = new GitHubRepository(owner, repo, $"https://github.com/{owner}/{repo}");
        return true;

        static bool IsInvalidSegmentChar(char c) => !(char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
    }
}
