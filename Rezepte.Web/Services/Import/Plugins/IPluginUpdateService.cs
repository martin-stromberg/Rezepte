namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the iplugin update service interface.
/// </summary>
public interface IPluginUpdateService
{
    /// <summary>
    /// Checks the for updates async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    Task CheckForUpdatesAsync(CancellationToken ct = default);
}
