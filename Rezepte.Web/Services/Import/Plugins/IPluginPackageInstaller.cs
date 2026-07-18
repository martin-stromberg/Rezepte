namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginPackageInstaller
{
    Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default);
    async Task InstallWithReloadTrackingAsync(IReadOnlyList<string> pluginDirectories, Func<CancellationToken, Task> beforeReload, CancellationToken ct = default)
    {
        await beforeReload(ct).ConfigureAwait(false);
        await InstallAsync(pluginDirectories, ct).ConfigureAwait(false);
    }
}
