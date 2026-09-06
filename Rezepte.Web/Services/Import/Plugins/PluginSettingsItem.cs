namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the settings item.
/// </summary>
/// <param name="PluginId">The plugin id parameter.</param>
/// <param name="DisplayName">The display name parameter.</param>
/// <param name="Description">The description parameter.</param>
/// <param name="AssemblyName">The assembly name parameter.</param>
/// <param name="TypeName">The type name parameter.</param>
/// <param name="Enabled">The enabled parameter.</param>
/// <param name="OrderIndex">The order index parameter.</param>
/// <param name="Status">The status parameter.</param>
/// <param name="Error">The error parameter.</param>
/// <param name="DiscoveredAt">The discovered at parameter.</param>
/// <param name="LastSeenAt">The last seen at parameter.</param>
/// <param name="Usability">The usability parameter.</param>
/// <returns>The result.</returns>
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
