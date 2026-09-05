namespace Rezepte.Web.Services.Updates;

/// <summary>
/// Represents the application update pre install handler class.
/// </summary>
public sealed class ApplicationUpdatePreInstallHandler : IApplicationUpdatePreInstallHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplicationUpdatePreInstallHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationUpdatePreInstallHandler"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    public ApplicationUpdatePreInstallHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ApplicationUpdatePreInstallHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs the pre install backup async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    public async Task RunPreInstallBackupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running pre-install update backup.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IUpdateBackupService>();
        var result = await backupService.CreateBackupAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Pre-install update backup completed at {BackupPath}", result.FilePath);
    }
}
