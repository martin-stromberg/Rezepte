namespace Rezepte.Web.Services.Updates;

public sealed class DisabledApplicationUpdater : IApplicationUpdater
{
    private readonly ILogger<DisabledApplicationUpdater> _logger;

    public DisabledApplicationUpdater(ILogger<DisabledApplicationUpdater> logger)
    {
        _logger = logger;
    }

    public Task RegisterPreInstallBackupAsync(Func<CancellationToken, Task> callback, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(callback);

        // External adapter point: replace this class with the real msTools.Updater binding once
        // package name, DI registration and awaitable pre-install hook semantics are verified.
        _logger.LogError("Application updater cannot be enabled because msTools.Updater API is not configured.");
        throw new InvalidOperationException(
            "Application updater cannot be enabled until a verified msTools.Updater adapter is configured.");
    }

    public Task CheckAndInstallUpdatesAsync(CancellationToken ct = default)
    {
        throw new InvalidOperationException(
            "Application updater is disabled. Configure a verified msTools.Updater adapter before installing updates.");
    }
}
