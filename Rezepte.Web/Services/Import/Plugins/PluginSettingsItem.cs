namespace Rezepte.Web.Services.Import.Plugins;

public sealed record PluginSettingsItem(
    string PluginId,
    string DisplayName,
    string? Description,
    string AssemblyName,
    string TypeName,
    bool Enabled,
    int OrderIndex,
    string Status,
    string? Error,
    DateTime DiscoveredAt,
    DateTime LastSeenAt,
    PluginUsabilityResult? Usability = null);
