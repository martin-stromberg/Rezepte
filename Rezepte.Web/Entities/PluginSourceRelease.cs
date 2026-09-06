using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the plugin source release class.
/// </summary>
public class PluginSourceRelease
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string PluginSourceId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public PluginSource? PluginSource { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string ReleaseTag { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public long GitHubReleaseId { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public long AssetId { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Status { get; set; } = PluginSourceReleaseStatus.Pending;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? DownloadedAt { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? ValidatedAt { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? InstalledAt { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? ReloadStatus { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? ReloadedAt { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? ReloadError { get; set; }
}
