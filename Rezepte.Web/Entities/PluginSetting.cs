using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Web.Entities;

public class PluginSetting
{
    public string PluginId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AssemblyName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int OrderIndex { get; set; }
    public string Status { get; set; } = PluginStatus.Loaded;
    public string? Error { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
