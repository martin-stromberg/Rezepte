namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the iplugin settings service interface.
/// </summary>
public interface IPluginSettingsService
{
    /// <summary>
    /// Gets the plugins async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<IReadOnlyList<PluginSettingsItem>> GetPluginsAsync(CancellationToken ct = default);
    /// <summary>
    /// Gets the sources async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<IReadOnlyList<PluginSourceSettingsItem>> GetSourcesAsync(CancellationToken ct = default);
    /// <summary>
    /// Saves the source async.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SaveSourceAsync(PluginSourceSaveRequest request, CancellationToken ct = default);
    /// <summary>
    /// Sets the source enabled async.
    /// </summary>
    /// <param name="sourceId">The source id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetSourceEnabledAsync(string sourceId, bool enabled, CancellationToken ct = default);
    /// <summary>
    /// Deletes the source async.
    /// </summary>
    /// <param name="sourceId">The source id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task DeleteSourceAsync(string sourceId, CancellationToken ct = default);
    /// <summary>
    /// Sets the enabled async.
    /// </summary>
    /// <param name="pluginId">The plugin id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetEnabledAsync(string pluginId, bool enabled, CancellationToken ct = default);
    /// <summary>
    /// moves the async.
    /// </summary>
    /// <param name="pluginId">The plugin id parameter.</param>
    /// <param name="direction">The direction parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task MoveAsync(string pluginId, int direction, CancellationToken ct = default);
}
