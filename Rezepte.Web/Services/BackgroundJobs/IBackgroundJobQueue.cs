namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// Defines the ibackground job queue interface.
/// </summary>
public interface IBackgroundJobQueue
{
    /// <summary>
    /// Enqueue a job. payloadJson can be null or a JSON string the handler expects.
    /// Returns job id.
    /// </summary>
    /// <param name="jobType">The job type parameter.</param>
    /// <param name="payloadJson">The payload json parameter.</param>
    /// <param name="initiatorUserId">The initiator user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    Task<Guid> EnqueueAsync(string jobType, string? payloadJson = null, string? initiatorUserId = null, CancellationToken ct = default);

    /// <summary>
    /// Query persisted job (or null).
    /// </summary>
    /// <param name="jobId">The job id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    Task<BackgroundJob?> GetJobAsync(Guid jobId, CancellationToken ct = default);
}
