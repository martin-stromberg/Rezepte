namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the iplugin manager interface.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Initializes the async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    Task InitializeAsync(CancellationToken ct = default);
    /// <summary>
    /// discovers the from directory.
    /// </summary>
    /// <param name="pluginRoot">The plugin root parameter.</param>
    /// <param name="unloadAfterDiscovery">The unload after discovery parameter.</param>
    /// <returns>The result.</returns>
    IReadOnlyList<ImportPluginDescriptor> DiscoverFromDirectory(string pluginRoot, bool unloadAfterDiscovery = false) => [];
    /// <summary>
    /// Gets the active handlers async.
    /// </summary>
    /// <param name="serviceProvider">The service provider parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default);
    /// <summary>
    /// acquires the active handlers async.
    /// </summary>
    /// <param name="serviceProvider">The service provider parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    async Task<PluginHandlerLease> AcquireActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => new(await GetActiveHandlersAsync(serviceProvider, ct).ConfigureAwait(false), null);

    /// <summary>
    /// reloads the async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    Task ReloadAsync(CancellationToken ct = default) => InitializeAsync(ct);
    /// <summary>
    /// coordinates the reload async.
    /// </summary>
    /// <param name="replacePlugins">The replace plugins parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    async Task CoordinateReloadAsync(Func<CancellationToken, Task> replacePlugins, CancellationToken ct = default)
    {
        await replacePlugins(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the plugins usability async.
    /// </summary>
    /// <param name="serviceProvider">The service provider parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<IReadOnlyDictionary<string, PluginUsabilityResult>> GetPluginsUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, PluginUsabilityResult>>(new Dictionary<string, PluginUsabilityResult>());
}
