namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// Implementieren für jeden Job‑Typ. Registriere Handler als Scoped/Transient.
/// Hander.JobType muss einzigartig sein.
/// </summary>
public interface IBackgroundJobHandler
{
    /// <summary>
    /// Eindeutiger Typname, z. B. "export:user" oder "export:all"
    /// </summary>
    string JobType { get; }

    /// <summary>
    /// Führe den Job aus. Der Handler wird innerhalb eines created IServiceScope aufgelöst (scoped services sind verfügbar).
    /// Der Handler erhält die BackgroundJob-Entity (persistiert) und CancellationToken.
    /// Der Handler soll Status/Progress in der DbContext aktualisieren (oder rely on UpdateProgressAsync helper).
    /// </summary>
    /// <param name="job">The job parameter.</param>
    /// <param name="scopeServices">The scope services parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    Task HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct);
}
