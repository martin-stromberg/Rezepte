namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the update hosted service.
/// </summary>
/// <param name="scopeFactory">The scope factory parameter.</param>
/// <param name="logger">The logger parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginUpdateHostedService(IServiceScopeFactory scopeFactory, ILogger<PluginUpdateHostedService> logger) : IHostedService
{
    /// <summary>
    /// starts the async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var updater = scope.ServiceProvider.GetRequiredService<IPluginUpdateService>();
            await updater.CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plugin update startup check failed.");
        }
    }

    /// <summary>
    /// stops the async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
