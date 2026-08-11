namespace Rezepte.Web.Services.Updates;

public sealed class ApplicationUpdatePreInstallHandler : IApplicationUpdatePreInstallHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplicationUpdatePreInstallHandler> _logger;

    public ApplicationUpdatePreInstallHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ApplicationUpdatePreInstallHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunPreInstallBackupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running pre-install update backup.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IUpdateBackupService>();
        var result = await backupService.CreateBackupAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Pre-install update backup completed at {BackupPath}", result.FilePath);
    }
}
