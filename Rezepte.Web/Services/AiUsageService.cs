using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data; // RezepteDbContext
using Rezepte.Web.Entities;
using Rezepte.Web.Services;

public class AiUsageService : IAiUsageService
{
    private readonly RezepteDbContext _db;
    public AiUsageService(RezepteDbContext db) { _db = db; }

    public async Task RecordRequestAsync(string userId, string serviceName, CancellationToken ct = default)
    {
        _db.AiRequestLogs.Add(new AiRequestLog { Id = Guid.NewGuid().ToString("n"), UserId = userId, Service = serviceName, Timestamp = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> GetCountAsync(string userId, CancellationToken ct = default)
    {
        return _db.AiRequestLogs.CountAsync(r => r.UserId == userId, ct);
    }
}