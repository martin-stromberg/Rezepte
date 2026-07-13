namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginManager
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default);
}
