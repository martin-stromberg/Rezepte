namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginStartupService(IPluginManager pluginManager) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => pluginManager.InitializeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
