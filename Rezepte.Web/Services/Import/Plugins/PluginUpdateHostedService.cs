namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginUpdateHostedService(IServiceScopeFactory scopeFactory, ILogger<PluginUpdateHostedService> logger) : IHostedService
{
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
