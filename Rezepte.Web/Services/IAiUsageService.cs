namespace Rezepte.Web.Services;
public interface IAiUsageService
{
    Task RecordRequestAsync(string userId, string serviceName, CancellationToken ct = default);
    Task<int> GetCountAsync(string userId, CancellationToken ct = default);
}