using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginSettingsService(RezepteDbContext db) : IPluginSettingsService
{
    public async Task<IReadOnlyList<PluginSettingsItem>> GetPluginsAsync(CancellationToken ct = default)
    {
        return await db.PluginSettings
            .AsNoTracking()
            .OrderBy(p => p.OrderIndex)
            .ThenBy(p => p.DisplayName)
            .Select(p => new PluginSettingsItem(
                p.PluginId,
                p.DisplayName,
                p.Description,
                p.AssemblyName,
                p.TypeName,
                p.Enabled,
                p.OrderIndex,
                p.Status,
                p.Error,
                p.DiscoveredAt,
                p.LastSeenAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task SetEnabledAsync(string pluginId, bool enabled, CancellationToken ct = default)
    {
        var plugin = await db.PluginSettings.FindAsync([pluginId], ct).ConfigureAwait(false);
        if (plugin is null)
        {
            throw new InvalidOperationException($"Plugin '{pluginId}' was not found.");
        }

        plugin.Enabled = enabled;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MoveAsync(string pluginId, int direction, CancellationToken ct = default)
    {
        if (direction == 0)
        {
            return;
        }

        var plugins = await db.PluginSettings
            .OrderBy(p => p.OrderIndex)
            .ThenBy(p => p.DisplayName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var index = plugins.FindIndex(p => p.PluginId == pluginId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Plugin '{pluginId}' was not found.");
        }

        var targetIndex = direction < 0 ? index - 1 : index + 1;
        if (targetIndex < 0 || targetIndex >= plugins.Count)
        {
            return;
        }

        (plugins[index].OrderIndex, plugins[targetIndex].OrderIndex) = (plugins[targetIndex].OrderIndex, plugins[index].OrderIndex);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
