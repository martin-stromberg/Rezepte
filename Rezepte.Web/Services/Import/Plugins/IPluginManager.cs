namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginManager
{
    Task InitializeAsync(CancellationToken ct = default);
    IReadOnlyList<ImportPluginDescriptor> DiscoverFromDirectory(string pluginRoot, bool unloadAfterDiscovery = false) => [];
    Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default);
}
