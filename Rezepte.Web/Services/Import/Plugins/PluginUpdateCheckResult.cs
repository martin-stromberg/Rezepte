namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the update check result.
/// </summary>
/// <param name="Outcome">The outcome parameter.</param>
/// <param name="ReleaseTag">The release tag parameter.</param>
/// <param name="Error">The error parameter.</param>
/// <param name="AlreadyInstalled">The already installed parameter.</param>
/// <returns>The result.</returns>
public sealed record PluginUpdateCheckResult(
    string Outcome,
    string? ReleaseTag,
    string? Error,
    bool AlreadyInstalled)
{
    /// <summary>
    /// Gets a value indicating whether the check completed without an error.
    /// </summary>
    /// <returns>The result.</returns>
    public bool IsSuccess => Outcome is not PluginUpdateCheckOutcome.Failed;

    /// <summary>
    /// Creates a result for a check that did not install a release.
    /// </summary>
    /// <param name="releaseTag">The release tag parameter.</param>
    /// <param name="alreadyInstalled">The already installed parameter.</param>
    /// <returns>The result.</returns>
    public static PluginUpdateCheckResult NoUpdate(string? releaseTag = null, bool alreadyInstalled = false)
        => new(PluginUpdateCheckOutcome.NoUpdate, releaseTag, null, alreadyInstalled);

    /// <summary>
    /// Creates a result for a check that installed a release.
    /// </summary>
    /// <param name="releaseTag">The release tag parameter.</param>
    /// <returns>The result.</returns>
    public static PluginUpdateCheckResult Installed(string releaseTag)
        => new(PluginUpdateCheckOutcome.Installed, releaseTag, null, false);

    /// <summary>
    /// Creates a result for a check that failed.
    /// </summary>
    /// <param name="error">The error parameter.</param>
    /// <param name="releaseTag">The release tag parameter.</param>
    /// <returns>The result.</returns>
    public static PluginUpdateCheckResult Failed(string error, string? releaseTag = null)
        => new(PluginUpdateCheckOutcome.Failed, releaseTag, error, false);
}
