namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the startup service.
/// </summary>
/// <param name="pluginManager">The plugin manager parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginStartupService(IPluginManager pluginManager) : IHostedService
{
    /// <summary>
    /// starts the async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    public Task StartAsync(CancellationToken cancellationToken) => pluginManager.InitializeAsync(cancellationToken);

    /// <summary>
    /// stops the async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
