namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the iplugin package installer interface.
/// </summary>
public interface IPluginPackageInstaller
{
    /// <summary>
    /// installs the async.
    /// </summary>
    /// <param name="pluginDirectories">The plugin directories parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default);
    /// <summary>
    /// installs the with reload tracking async.
    /// </summary>
    /// <param name="pluginDirectories">The plugin directories parameter.</param>
    /// <param name="beforeReload">The before reload parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    async Task InstallWithReloadTrackingAsync(IReadOnlyList<string> pluginDirectories, Func<CancellationToken, Task> beforeReload, CancellationToken ct = default)
    {
        await beforeReload(ct).ConfigureAwait(false);
        await InstallAsync(pluginDirectories, ct).ConfigureAwait(false);
    }
}
