namespace Rezepte.Web.Configuration;

/// <summary>
/// Represents the application update options class.
/// </summary>
public sealed class ApplicationUpdateOptions
{
    /// <summary>
    /// Enables automatic update runs in msTools.Updater.
    /// Manual checks remain possible when set to <see langword="false"/>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Automatically downloads newly found updates.
    /// </summary>
    public bool EnableAutomaticDownload { get; set; } = true;

    /// <summary>
    /// Automatically installs downloaded updates.
    /// </summary>
    public bool EnableAutomaticInstallation { get; set; }

    /// <summary>
    /// Allows pre-release versions to be considered during update checks.
    /// </summary>
    public bool AllowPrereleaseUpdates { get; set; }

    /// <summary>
    /// Local working directory for update packages, status files and locks.
    /// </summary>
    public string DownloadPath { get; set; } = "updates";

    /// <summary>
    /// Enables the background services of msTools.Updater.
    /// </summary>
    public bool HostedServicesEnabled { get; set; } = true;

    /// <summary>
    /// Stops the host after the installation script has been started.
    /// Required on Linux so that the running process does not block file replacement.
    /// </summary>
    public bool StopHostAfterScriptStart { get; set; }

    /// <summary>
    /// Health and lock timeout in seconds used by the updater.
    /// </summary>
    public int HealthTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Linux only: systemd unit name used to run the installation script.
    /// </summary>
    public string UpdateUnitName { get; set; } = "RezepteWebAutoUpdate";

    /// <summary>
    /// GitHub repository owner for the release source.
    /// </summary>
    public string? RepositoryOwner { get; set; }

    /// <summary>
    /// GitHub repository name for the release source.
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Asset name of the update manifest in the GitHub release.
    /// </summary>
    public string? ManifestAssetName { get; set; }

    /// <summary>
    /// Path to a local folder used as an alternative update source.
    /// </summary>
    public string? LocalSourceDirectory { get; set; }

    /// <summary>
    /// Windows only: name of the IIS application pool to stop and restart during installation.
    /// </summary>
    public string? AppPoolName { get; set; }

    /// <summary>
    /// Windows only: optional name of the IIS site, used for logging when <see cref="AppPoolName"/> is set.
    /// </summary>
    public string? SiteName { get; set; }

    /// <summary>
    /// Windows only: name of the service to stop and restart during installation.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Windows only: path to the executable to restart, if no service is configured.
    /// </summary>
    public string? ExecutablePath { get; set; }
}
