namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Well-known error texts produced by the plugin update check. Shared between
/// <see cref="PluginUpdateService"/> and the UI so the settings component can map them to
/// localized messages without duplicating string literals.
/// </summary>
public static class PluginUpdateCheckErrors
{
    /// <summary>
    /// The check was requested for a source id that does not exist.
    /// </summary>
    public const string SourceNotFound = "Plugin source was not found.";

    /// <summary>
    /// The check was requested for a source that is not enabled.
    /// </summary>
    public const string SourceDisabled = "Plugin source is disabled.";

    /// <summary>
    /// The check was requested for a source whose trust is not confirmed.
    /// </summary>
    public const string SourceNotTrusted = "Plugin source is not confirmed as trusted.";

    /// <summary>
    /// The source is private but no personal access token is stored for it.
    /// </summary>
    public const string PrivateSourceNoPat = "Private source has no stored PAT.";

    /// <summary>
    /// The repository has no published GitHub release.
    /// </summary>
    public const string NoPublishedRelease = "No published GitHub release was found.";

    /// <summary>
    /// The latest GitHub release does not contain a ZIP asset.
    /// </summary>
    public const string NoZipAsset = "Latest release has no ZIP asset.";

    /// <summary>
    /// The plugin package validation failed without a more specific error.
    /// </summary>
    public const string ValidationFailed = "Plugin package validation failed.";

    /// <summary>
    /// The GitHub API rate limit was reached.
    /// </summary>
    public const string RateLimited = "GitHub rate limit reached.";

    /// <summary>
    /// No temporary work directory could be created for the update check.
    /// </summary>
    public const string WorkDirectoryUnavailable = "A temporary work directory could not be created.";
}
