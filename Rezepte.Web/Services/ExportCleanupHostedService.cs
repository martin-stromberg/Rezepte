namespace Rezepte.Web.Services;

/// <summary>
/// Periodically checks whether the daily export cleanup is due and runs it.
/// Because the check compares the last completed run against the most recent scheduled
/// occurrence, a run that was missed while the application was offline is caught up on
/// the first check after startup.
/// </summary>
public sealed class ExportCleanupHostedService : BackgroundService
{
    /// <summary>
    /// times the span.
    /// </summary>
    /// <returns>The result.</returns>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExportCleanupHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportCleanupHostedService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider parameter.</param>
    /// <param name="timeProvider">The time provider parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    public ExportCleanupHostedService(
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<ExportCleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// executes the async.
    /// </summary>
    /// <param name="stoppingToken">The stopping token parameter.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExportCleanupHostedService starting.");

        using var timer = new PeriodicTimer(CheckInterval, _timeProvider);
        try
        {
            do
            {
                await RunIfDueAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { }

        _logger.LogInformation("ExportCleanupHostedService stopping.");
    }

    /// <summary>
    /// Runs the if due async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    public async Task RunIfDueAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cleanup = scope.ServiceProvider.GetRequiredService<IExportCleanupService>();
            var now = _timeProvider.GetLocalNow();

            if (!await cleanup.IsCleanupDueAsync(now, ct).ConfigureAwait(false))
            {
                return;
            }

            _logger.LogInformation("Export cleanup is due; starting run.");
            await cleanup.RunCleanupAsync(now, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export cleanup run failed.");
        }
    }
}
