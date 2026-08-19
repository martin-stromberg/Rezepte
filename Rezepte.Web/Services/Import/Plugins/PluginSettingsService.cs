using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginSettingsService(
    RezepteDbContext db,
    IPluginManager pluginManager,
    IServiceProvider serviceProvider,
    IHttpContextAccessor? httpContextAccessor = null,
    ISystemSecretStore? secretStore = null) : IPluginSettingsService
{
    public async Task<IReadOnlyList<PluginSettingsItem>> GetPluginsAsync(CancellationToken ct = default)
    {
        var items = await db.PluginSettings
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

        var usability = await pluginManager.GetPluginsUsabilityAsync(serviceProvider, ct).ConfigureAwait(false);
        return items
            .Select(item => usability.TryGetValue(item.PluginId, out var result)
                ? item with { Usability = result }
                : item)
            .ToList();
    }

    public async Task<IReadOnlyList<PluginSourceSettingsItem>> GetSourcesAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        return await db.PluginSources
            .AsNoTracking()
            .OrderBy(s => s.Owner)
            .ThenBy(s => s.Repository)
            .Select(s => new PluginSourceSettingsItem(
                s.Id,
                s.RepositoryUrl,
                s.Owner,
                s.Repository,
                s.IsPrivate,
                s.Enabled,
                s.TrustConfirmed,
                !string.IsNullOrWhiteSpace(s.SecretName),
                s.LastSuccessfulReleaseTag,
                s.LastError,
                s.LastCheckedAt,
                s.LastErrorAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task SaveSourceAsync(PluginSourceSaveRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var repository = GitHubRepository.Parse(request.RepositoryUrl);
        var now = DateTime.UtcNow;
        var isNew = string.IsNullOrWhiteSpace(request.Id);
        if (isNew && !request.TrustConfirmed)
        {
            throw new InvalidOperationException("Neue Pluginquellen müssen als vertrauenswürdig bestätigt werden.");
        }

        var duplicate = await db.PluginSources
            .FirstOrDefaultAsync(s => s.Owner == repository.Owner && s.Repository == repository.Repository && s.Id != request.Id, ct)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            throw new InvalidOperationException("Diese GitHub-Quelle ist bereits konfiguriert.");
        }

        PluginSource source;
        if (isNew)
        {
            source = new PluginSource { Id = Guid.NewGuid().ToString("N"), CreatedAt = now };
            db.PluginSources.Add(source);
        }
        else
        {
            source = await db.PluginSources.FindAsync([request.Id], ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Pluginquelle wurde nicht gefunden.");
        }

        source.RepositoryUrl = repository.CanonicalUrl;
        source.Owner = repository.Owner;
        source.Repository = repository.Repository;
        source.IsPrivate = request.IsPrivate;
        source.Enabled = request.Enabled;
        source.TrustConfirmed = request.TrustConfirmed || source.TrustConfirmed;
        source.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(request.PersonalAccessToken))
        {
            if (secretStore is null)
            {
                throw new InvalidOperationException("Secret-Storage ist nicht verfügbar.");
            }

            source.SecretName ??= $"plugin-source-{source.Id}-pat";
            await secretStore.StoreAsync(source.SecretName, request.PersonalAccessToken, ct).ConfigureAwait(false);
        }
        else if (!source.IsPrivate && !string.IsNullOrWhiteSpace(source.SecretName) && secretStore is not null)
        {
            await secretStore.DeleteAsync(source.SecretName, ct).ConfigureAwait(false);
            source.SecretName = null;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetSourceEnabledAsync(string sourceId, bool enabled, CancellationToken ct = default)
    {
        EnsureAdmin();
        var source = await db.PluginSources.FindAsync([sourceId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pluginquelle wurde nicht gefunden.");
        source.Enabled = enabled;
        source.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteSourceAsync(string sourceId, CancellationToken ct = default)
    {
        EnsureAdmin();
        var source = await db.PluginSources.FindAsync([sourceId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pluginquelle wurde nicht gefunden.");
        if (!string.IsNullOrWhiteSpace(source.SecretName) && secretStore is not null)
        {
            await secretStore.DeleteAsync(source.SecretName, ct).ConfigureAwait(false);
        }

        db.PluginSources.Remove(source);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
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

    private void EnsureAdmin()
    {
        var user = httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Nur Administratoren dürfen Pluginquellen verwalten.");
        }

        if (!(user.IsInRole("Admin") || user.HasClaim("IsAdmin", "true")))
        {
            throw new UnauthorizedAccessException("Nur Administratoren dürfen Pluginquellen verwalten.");
        }
    }
}
