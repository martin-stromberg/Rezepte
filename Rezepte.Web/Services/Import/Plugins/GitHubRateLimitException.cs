namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// gits the hub rate limit exception.
/// </summary>
/// <param name="message">The message parameter.</param>
/// <param name="retryAfter">The retry after parameter.</param>
/// <returns>The result.</returns>
public sealed class GitHubRateLimitException(string message, TimeSpan? retryAfter = null) : HttpRequestException(message)
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
