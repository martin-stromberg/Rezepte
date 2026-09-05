namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Represents the plugin source release status class.
/// </summary>
public static class PluginSourceReleaseStatus
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Pending = "Pending";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Downloading = "Downloading";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string DownloadFailed = "DownloadFailed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string RateLimited = "RateLimited";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Validating = "Validating";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string ValidationFailed = "ValidationFailed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Installing = "Installing";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string InstallFailed = "InstallFailed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Reloading = "Reloading";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string ReloadFailed = "ReloadFailed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Installed = "Installed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Skipped = "Skipped";
}
