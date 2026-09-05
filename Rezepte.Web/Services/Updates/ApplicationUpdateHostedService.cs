using Microsoft.Extensions.Options;
using msTools.Updater;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services.Updates;

/// <summary>
/// Represents the application update hosted service class.
/// </summary>
public sealed class ApplicationUpdateHostedService : IHostedService
{
    private readonly IAutoUpdateEventAggregator _events;
    private readonly IApplicationUpdatePreInstallHandler _preInstallHandler;
    private readonly IOptions<ApplicationUpdateOptions> _options;
    private readonly ILogger<ApplicationUpdateHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationUpdateHostedService"/> class.
    /// </summary>
    /// <param name="events">The events parameter.</param>
    /// <param name="preInstallHandler">The pre install handler parameter.</param>
    /// <param name="options">The options parameter.</param>
    /// <param name="logger">The logger parameter.</param>
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

    /// <summary>
    /// starts the async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("Application updates are disabled by configuration.");
            return;
        }

        _events.BeforeCheckSource += OnBeforeCheckSource;
        _events.BeforeInstall += OnBeforeInstall;
        _events.ErrorOccurred += OnErrorOccurred;

        await Task.CompletedTask;
    }

    /// <summary>
    /// stops the async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _events.BeforeCheckSource -= OnBeforeCheckSource;
        _events.BeforeInstall -= OnBeforeInstall;
        _events.ErrorOccurred -= OnErrorOccurred;
        return Task.CompletedTask;
    }

    private void OnBeforeCheckSource(object? sender, AutoUpdateCancelEventArgs e)
    {
        _logger.LogDebug("Checking for application updates.");
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
