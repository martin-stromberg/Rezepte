namespace Rezepte.Web.Services.Updates;

/// <summary>
/// Defines the iapplication update pre install handler interface.
/// </summary>
public interface IApplicationUpdatePreInstallHandler
{
    /// <summary>
    /// Runs the pre install backup async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    Task RunPreInstallBackupAsync(CancellationToken ct = default);
}
