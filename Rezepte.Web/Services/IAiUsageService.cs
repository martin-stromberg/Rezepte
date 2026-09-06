using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

/// <summary>
/// Defines the iai usage service interface.
/// </summary>
public interface IAiUsageService
{
    /// <summary>
    /// Record a usage entry. By default an entry is of type Request.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="serviceName">The service name parameter.</param>
    /// <param name="type">The type parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    Task RecordRequestAsync(string userId, string serviceName, AiRequestLogType type = AiRequestLogType.Request, CancellationToken ct = default);

    /// <summary>
    /// Try to record a request while enforcing global limits. Returns false if the request must be blocked.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="serviceName">The service name parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    Task<bool> TryRecordRequestAsync(string userId, string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Count recent request-type entries for a user (e.g. last 24h).
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    Task<int> GetCountAsync(string userId, CancellationToken ct = default);
}
