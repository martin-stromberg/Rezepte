using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;
public interface IAiUsageService
{
    /// <summary>
    /// Record a usage entry. By default an entry is of type Request.
    /// </summary>
    Task RecordRequestAsync(string userId, string serviceName, AiRequestLogType type = AiRequestLogType.Request, CancellationToken ct = default);

    /// <summary>
    /// Try to record a request while enforcing global limits. Returns false if the request must be blocked.
    /// </summary>
    Task<bool> TryRecordRequestAsync(string userId, string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Count recent request-type entries for a user (e.g. last 24h).
    /// </summary>
    Task<int> GetCountAsync(string userId, CancellationToken ct = default);
}