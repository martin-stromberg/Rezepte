using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

public class SettingsService : ISettingsService
{
    private readonly RezepteDbContext _db;

    public SettingsService(RezepteDbContext db)
    {
        _db = db;
    }

    public async Task<bool> GetUserAiEnabledAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return true;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.AiEnabled ?? true;
    }

    public async Task SetUserAiEnabledAsync(string userId, bool enabled, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        var set = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        if (set == null)
        {
            set = new UserSetting { UserId = userId, AiEnabled = enabled };
            _db.Add(set);
        }
        else
        {
            set.AiEnabled = enabled;
            _db.Update(set);
        }
        await _db.SaveChangesAsync(ct);
    }

    private const string AiKey = "AiEnabled";

    public async Task<bool> GetGlobalAiEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { AiKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    public async Task SetGlobalAiEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { AiKey }, ct);
        if (kv == null)
        {
            kv = new AppSetting { Key = AiKey, Value = enabled.ToString() };
            _db.Add(kv);
        }
        else
        {
            kv.Value = enabled.ToString();
            _db.Update(kv);
        }
        await _db.SaveChangesAsync(ct);
    }
}