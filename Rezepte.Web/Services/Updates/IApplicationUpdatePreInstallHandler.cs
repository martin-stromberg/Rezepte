namespace Rezepte.Web.Services.Updates;

public interface IApplicationUpdatePreInstallHandler
{
    Task RunPreInstallBackupAsync(CancellationToken ct = default);
}
