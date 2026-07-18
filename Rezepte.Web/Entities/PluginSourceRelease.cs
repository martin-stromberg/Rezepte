using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Web.Entities;

public class PluginSourceRelease
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PluginSourceId { get; set; } = string.Empty;
    public PluginSource? PluginSource { get; set; }
    public string ReleaseTag { get; set; } = string.Empty;
    public long GitHubReleaseId { get; set; }
    public long AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string Status { get; set; } = PluginSourceReleaseStatus.Pending;
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DownloadedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? InstalledAt { get; set; }
}
