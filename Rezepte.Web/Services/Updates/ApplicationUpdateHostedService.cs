using Microsoft.Extensions.Options;
using msTools.Updater;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services.Updates;

public sealed class ApplicationUpdateHostedService : IHostedService
{
    private readonly IAutoUpdateEventAggregator _events;
    private readonly IApplicationUpdatePreInstallHandler _preInstallHandler;
    private readonly IOptions<ApplicationUpdateOptions> _options;
    private readonly ILogger<ApplicationUpdateHostedService> _logger;

    public ApplicationUpdateHostedService(
        IAutoUpdateEventAggregator events,
        IApplicationUpdatePreInstallHandler preInstallHandler,
        IOptions<ApplicationUpdateOptions> options,
        ILogger<ApplicationUpdateHostedService> logger)
    {
        _events = events;
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

        _events.BeforeInstall += OnBeforeInstall;
        _events.ErrorOccurred += OnErrorOccurred;

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _events.BeforeInstall -= OnBeforeInstall;
        _events.ErrorOccurred -= OnErrorOccurred;
        return Task.CompletedTask;
    }

    private void OnBeforeInstall(object? sender, BeforeInstallEventArgs args)
    {
        try
        {
            _preInstallHandler.RunPreInstallBackupAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            _logger.LogError(
                ex,
                "Pre-install update backup failed for package {PackagePath}. Installation is canceled.",
                args.PackageFile.FullName);
        }
    }

    private void OnErrorOccurred(object? sender, AutoUpdateErrorEventArgs args)
    {
        _logger.LogError(args.Error, "Application update failed during {Phase}.", args.Phase);
    }
}
