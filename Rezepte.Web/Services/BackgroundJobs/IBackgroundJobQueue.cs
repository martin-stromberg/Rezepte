namespace Rezepte.Web.Services.BackgroundJobs;

public interface IBackgroundJobQueue
{
    /// <summary>
    /// Enqueue a job. payloadJson can be null or a JSON string the handler expects.
    /// Returns job id.
    /// </summary>
    Task<Guid> EnqueueAsync(string jobType, string? payloadJson = null, string? initiatorUserId = null, CancellationToken ct = default);

    /// <summary>
    /// Query persisted job (or null).
    /// </summary>
    Task<BackgroundJob?> GetJobAsync(Guid jobId, CancellationToken ct = default);
}