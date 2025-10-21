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
    private const string GoogleVisionKey = "GlobalGoogleVisionEnabled";
    private const string GeminiKey = "GlobalGeminiEnabled";

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

    // per-user service toggles
    public async Task<bool> GetUserGoogleVisionEnabledAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return true;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.GoogleVisionEnabled ?? true;
    }

    public async Task SetUserGoogleVisionEnabledAsync(string userId, bool enabled, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        var set = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        if (set == null)
        {
            set = new UserSetting { UserId = userId, GoogleVisionEnabled = enabled };
            _db.Add(set);
        }
        else
        {
            set.GoogleVisionEnabled = enabled;
            _db.Update(set);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> GetUserGeminiEnabledAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return true;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.GeminiEnabled ?? true;
    }

    public async Task SetUserGeminiEnabledAsync(string userId, bool enabled, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        var set = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        if (set == null)
        {
            set = new UserSetting { UserId = userId, GeminiEnabled = enabled };
            _db.Add(set);
        }
        else
        {
            set.GeminiEnabled = enabled;
            _db.Update(set);
        }
        await _db.SaveChangesAsync(ct);
    }

    // New: per-user confirmation requirement
    public async Task<bool> GetUserRequireAiConfirmationAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.RequireAiConfirmation ?? false;
    }

    public async Task SetUserRequireAiConfirmationAsync(string userId, bool required, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        var set = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        if (set == null)
        {
            set = new UserSetting { UserId = userId, RequireAiConfirmation = required };
            _db.Add(set);
        }
        else
        {
            set.RequireAiConfirmation = required;
            _db.Update(set);
        }
        await _db.SaveChangesAsync(ct);
    }

    // global per-service toggles
    public async Task<bool> GetGlobalGoogleVisionEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GoogleVisionKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    public async Task SetGlobalGoogleVisionEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GoogleVisionKey }, ct);
        if (kv == null)
        {
            kv = new AppSetting { Key = GoogleVisionKey, Value = enabled.ToString() };
            _db.Add(kv);
        }
        else
        {
            kv.Value = enabled.ToString();
            _db.Update(kv);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> GetGlobalGeminiEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GeminiKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    public async Task SetGlobalGeminiEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GeminiKey }, ct);
        if (kv == null)
        {
            kv = new AppSetting { Key = GeminiKey, Value = enabled.ToString() };
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