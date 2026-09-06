namespace Rezepte.Web.Configuration;

/// <summary>
/// Represents the plugin update options class.
/// </summary>
public sealed class PluginUpdateOptions
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string GitHubApiBaseUrl { get; set; } = "https://api.github.com";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserAgent { get; set; } = "Rezepte.PluginUpdater";
}
