using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services.Updates;

public sealed class ApplicationUpdateHostedService : IHostedService
{
    private readonly IApplicationUpdater _updater;
    private readonly IApplicationUpdatePreInstallHandler _preInstallHandler;
    private readonly IOptions<ApplicationUpdateOptions> _options;
    private readonly ILogger<ApplicationUpdateHostedService> _logger;

    public ApplicationUpdateHostedService(
        IApplicationUpdater updater,
        IApplicationUpdatePreInstallHandler preInstallHandler,
        IOptions<ApplicationUpdateOptions> options,
        ILogger<ApplicationUpdateHostedService> logger)
    {
        _updater = updater;
        _preInstallHandler = preInstallHandler;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("Application updates are disabled by configuration.");
            return;
        }

        await _updater
            .RegisterPreInstallBackupAsync(_preInstallHandler.RunPreInstallBackupAsync, cancellationToken)
            .ConfigureAwait(false);

        if (options.CheckOnStartup)
        {
            await _updater.CheckAndInstallUpdatesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
