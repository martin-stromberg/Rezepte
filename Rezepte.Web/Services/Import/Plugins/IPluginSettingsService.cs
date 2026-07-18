namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginSettingsService
{
    Task<IReadOnlyList<PluginSettingsItem>> GetPluginsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PluginSourceSettingsItem>> GetSourcesAsync(CancellationToken ct = default);
    Task SaveSourceAsync(PluginSourceSaveRequest request, CancellationToken ct = default);
    Task SetSourceEnabledAsync(string sourceId, bool enabled, CancellationToken ct = default);
    Task DeleteSourceAsync(string sourceId, CancellationToken ct = default);
    Task SetEnabledAsync(string pluginId, bool enabled, CancellationToken ct = default);
    Task MoveAsync(string pluginId, int direction, CancellationToken ct = default);
}
