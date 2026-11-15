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
    /// <summary>
    /// Checks a single plugin source for updates.
    /// </summary>
    /// <param name="sourceId">The source id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<PluginUpdateCheckResult> CheckSourceForUpdatesAsync(string sourceId, CancellationToken ct = default);
}
