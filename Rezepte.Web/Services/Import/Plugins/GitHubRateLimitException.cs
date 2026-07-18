namespace Rezepte.Web.Services.Import.Plugins;

public sealed class GitHubRateLimitException(string message, TimeSpan? retryAfter = null) : HttpRequestException(message)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
