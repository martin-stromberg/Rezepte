namespace Rezepte.Web.Services.Import.Plugins;

public static class PluginSourceReleaseStatus
{
    public const string Pending = "Pending";
    public const string Downloading = "Downloading";
    public const string DownloadFailed = "DownloadFailed";
    public const string RateLimited = "RateLimited";
    public const string Validating = "Validating";
    public const string ValidationFailed = "ValidationFailed";
    public const string Installing = "Installing";
    public const string InstallFailed = "InstallFailed";
    public const string Reloading = "Reloading";
    public const string ReloadFailed = "ReloadFailed";
    public const string Installed = "Installed";
    public const string Skipped = "Skipped";
}
