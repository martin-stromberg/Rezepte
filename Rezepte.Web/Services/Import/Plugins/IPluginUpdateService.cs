namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginUpdateService
{
    Task CheckForUpdatesAsync(CancellationToken ct = default);
}
