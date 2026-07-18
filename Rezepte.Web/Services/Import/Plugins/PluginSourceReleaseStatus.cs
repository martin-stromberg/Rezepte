namespace Rezepte.Web.Services.Import.Plugins;

public static class PluginSourceReleaseStatus
{
    public const string Pending = "Pending";
    public const string Downloading = "Downloading";
    public const string DownloadFailed = "DownloadFailed";
    public const string Validating = "Validating";
    public const string ValidationFailed = "ValidationFailed";
    public const string Installing = "Installing";
    public const string InstallFailed = "InstallFailed";
    public const string Installed = "Installed";
    public const string Skipped = "Skipped";
}
