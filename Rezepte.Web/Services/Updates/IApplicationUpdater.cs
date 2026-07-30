namespace Rezepte.Web.Services.Updates;

public interface IApplicationUpdater
{
    Task RegisterPreInstallBackupAsync(Func<CancellationToken, Task> callback, CancellationToken ct = default);
    Task CheckAndInstallUpdatesAsync(CancellationToken ct = default);
}
