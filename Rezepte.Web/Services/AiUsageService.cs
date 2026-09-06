using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the ai usage service class.
/// </summary>
public class AiUsageService : IAiUsageService
{
    private readonly RezepteDbContext _db;
    private readonly ISettingsService _settings;
    private readonly ILogger<AiUsageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiUsageService"/> class.
    /// </summary>
    /// <param name="db">The db parameter.</param>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    public AiUsageService(RezepteDbContext db, ISettingsService settings, ILogger<AiUsageService> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// records the request async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="serviceName">The service name parameter.</param>
    /// <param name="type">The type parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task RecordRequestAsync(string userId, string serviceName, AiRequestLogType type = AiRequestLogType.Request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var log = new AiRequestLog
        {
            UserId = userId ?? string.Empty,
            Service = serviceName ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Type = type
        };
        _db.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Tries to record request async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="serviceName">The service name parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> TryRecordRequestAsync(string userId, string serviceName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var maxPerHour = await _settings.GetGlobalMaxRequestsPerHourAsync(ct);
        var maxPerDay = await _settings.GetGlobalMaxRequestsPerDayAsync(ct);
        var disableOnLimit = await _settings.GetGlobalDisableOnLimitReachedAsync(ct);

        // Treat null or <=0 as unlimited
        var limitHour = (maxPerHour.HasValue && maxPerHour.Value > 0) ? maxPerHour.Value : (int?)null;
        var limitDay = (maxPerDay.HasValue && maxPerDay.Value > 0) ? maxPerDay.Value : (int?)null;

        var now = DateTime.UtcNow;
        bool reached = false;

        if (limitHour.HasValue)
        {
            var sinceHour = now.AddHours(-1);
            var countHour = await _db.Set<AiRequestLog>().CountAsync(l => l.Timestamp >= sinceHour && l.Type == AiRequestLogType.Request, ct);
            if (countHour >= limitHour.Value) reached = true;
        }

        if (!reached && limitDay.HasValue)
        {
            var sinceDay = now.AddDays(-1);
            var countDay = await _db.Set<AiRequestLog>().CountAsync(l => l.Timestamp >= sinceDay && l.Type == AiRequestLogType.Request, ct);
            if (countDay >= limitDay.Value) reached = true;
        }

        if (reached)
        {
            _logger.LogWarning("AI usage limit reached (hour:{MaxHour} day:{MaxDay}). User={UserId} Service={Service}", maxPerHour, maxPerDay, userId, serviceName);
            if (disableOnLimit)
            {
                try
                {
                    await _settings.SetGlobalAiEnabledAsync(false, ct);
                    _logger.LogInformation("Global AI disabled due to limit reached.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to disable global AI setting after limit reached.");
                }
            }
            return false;
        }

        // Not reached: record ENTRY as Request and allow
        var log = new AiRequestLog
        {
            UserId = userId ?? string.Empty,
            Service = serviceName ?? string.Empty,
            Timestamp = now,
            Type = AiRequestLogType.Request
        };
        _db.Add(log);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Gets the count async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<int> GetCountAsync(string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var since = DateTime.UtcNow.AddDays(-1);
        return await _db.Set<AiRequestLog>().CountAsync(l => l.UserId == (userId ?? string.Empty) && l.Timestamp >= since && l.Type == AiRequestLogType.Request, ct);
    }
}
