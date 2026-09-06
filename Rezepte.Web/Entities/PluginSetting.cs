using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the plugin setting class.
/// </summary>
public class PluginSetting
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string PluginId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int OrderIndex { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Status { get; set; } = PluginStatus.Loaded;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
