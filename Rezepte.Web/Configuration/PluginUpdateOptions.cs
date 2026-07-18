namespace Rezepte.Web.Configuration;

public sealed class PluginUpdateOptions
{
    public string GitHubApiBaseUrl { get; set; } = "https://api.github.com";
    public int TimeoutSeconds { get; set; } = 30;
    public string UserAgent { get; set; } = "Rezepte.PluginUpdater";
}
