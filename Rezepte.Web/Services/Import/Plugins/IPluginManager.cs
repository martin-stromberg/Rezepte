namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginManager
{
    Task InitializeAsync(CancellationToken ct = default);
    IReadOnlyList<ImportPluginDescriptor> DiscoverFromDirectory(string pluginRoot, bool unloadAfterDiscovery = false) => [];
    Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default);
    async Task<PluginHandlerLease> AcquireActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => new(await GetActiveHandlersAsync(serviceProvider, ct).ConfigureAwait(false), null);

    Task ReloadAsync(CancellationToken ct = default) => InitializeAsync(ct);
    async Task CoordinateReloadAsync(Func<CancellationToken, Task> replacePlugins, CancellationToken ct = default)
    {
        await replacePlugins(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);
    }

    Task<IReadOnlyDictionary<string, PluginUsabilityResult>> GetPluginsUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, PluginUsabilityResult>>(new Dictionary<string, PluginUsabilityResult>());
}
