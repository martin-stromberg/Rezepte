using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Dtos;
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
        await UpdateUserSettingAsync(userId, s => s.AiEnabled = enabled, ct);
    }

    private const string AiKey = "AiEnabled";
    private const string GoogleVisionKey = "GlobalGoogleVisionEnabled";
    private const string GeminiKey = "GlobalGeminiEnabled";

    // new keys
    private const string GlobalMaxPerHourKey = "GlobalMaxRequestsPerHour";
    private const string GlobalMaxPerDayKey = "GlobalMaxRequestsPerDay";
    private const string GlobalDisableOnLimitKey = "GlobalDisableOnLimitReached";
    private const string ShoppingListEditModePrefix = "ShoppingListEditMode:";

    private const string SecurityTxtEnabledKey = "SecurityTxt.Enabled";
    private const string SecurityTxtContactKey = "SecurityTxt.Contact";
    private const string SecurityTxtExpiresKey = "SecurityTxt.Expires";
    private const string SecurityTxtEncryptionKey = "SecurityTxt.Encryption";
    private const string SecurityTxtAcknowledgmentsKey = "SecurityTxt.Acknowledgments";
    private const string SecurityTxtPreferredLanguagesKey = "SecurityTxt.PreferredLanguages";
    private const string SecurityTxtCanonicalKey = "SecurityTxt.Canonical";
    private const string SecurityTxtPolicyKey = "SecurityTxt.Policy";
    private const string SecurityTxtHiringKey = "SecurityTxt.Hiring";

    private static readonly string[] SecurityTxtKeys =
    {
        SecurityTxtEnabledKey, SecurityTxtContactKey, SecurityTxtExpiresKey,
        SecurityTxtEncryptionKey, SecurityTxtAcknowledgmentsKey, SecurityTxtPreferredLanguagesKey,
        SecurityTxtCanonicalKey, SecurityTxtPolicyKey, SecurityTxtHiringKey
    };

    public async Task<bool> GetGlobalAiEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { AiKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    public async Task SetGlobalAiEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        await WriteBool(AiKey, enabled, ct);
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
        await UpdateUserSettingAsync(userId, s => s.GoogleVisionEnabled = enabled, ct);
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
        await UpdateUserSettingAsync(userId, s => s.GeminiEnabled = enabled, ct);
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
        await UpdateUserSettingAsync(userId, s => s.RequireAiConfirmation = required, ct);
    }

    public async Task<bool> GetUserShoppingListEditModeAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { ShoppingListEditModeKey(userId) }, ct);
        return kv is not null && bool.TryParse(kv.Value, out var value) && value;
    }

    public async Task SetUserShoppingListEditModeAsync(string userId, bool editMode, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        await WriteString(ShoppingListEditModeKey(userId), editMode.ToString(), ct);
    }

    private static string ShoppingListEditModeKey(string userId) => ShoppingListEditModePrefix + userId;

    // global per-service toggles
    public async Task<bool> GetGlobalGoogleVisionEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GoogleVisionKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    public async Task SetGlobalGoogleVisionEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        await WriteBool(GoogleVisionKey, enabled, ct);
    }

    public async Task<bool> GetGlobalGeminiEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GeminiKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    public async Task SetGlobalGeminiEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        await WriteBool(GeminiKey, enabled, ct);
    }

    // New: global limits implementation
    public async Task<int?> GetGlobalMaxRequestsPerHourAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GlobalMaxPerHourKey }, ct);
        if (kv == null) return null;
        if (int.TryParse(kv.Value, out var v)) return v;
        return null;
    }

    public async Task SetGlobalMaxRequestsPerHourAsync(int? value, CancellationToken ct = default)
    {
        await WriteNullableInt(GlobalMaxPerHourKey, value, ct);
    }

    public async Task<int?> GetGlobalMaxRequestsPerDayAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GlobalMaxPerDayKey }, ct);
        if (kv == null) return null;
        if (int.TryParse(kv.Value, out var v)) return v;
        return null;
    }

    public async Task SetGlobalMaxRequestsPerDayAsync(int? value, CancellationToken ct = default)
    {
        await WriteNullableInt(GlobalMaxPerDayKey, value, ct);
    }

    public async Task<bool> GetGlobalDisableOnLimitReachedAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GlobalDisableOnLimitKey }, ct);
        if (kv == null) return false;
        return bool.TryParse(kv.Value, out var v) ? v : false;
    }

    public async Task SetGlobalDisableOnLimitReachedAsync(bool disable, CancellationToken ct = default)
    {
        await WriteBool(GlobalDisableOnLimitKey, disable, ct);
    }

    public async Task<SecurityTxtSettings> GetSecurityTxtSettingsAsync(CancellationToken ct = default)
    {
        var rows = await _db.Set<AppSetting>()
            .Where(s => SecurityTxtKeys.Contains(s.Key))
            .ToListAsync(ct);

        string? Get(string key) => rows.FirstOrDefault(r => r.Key == key)?.Value;

        var enabledRaw = Get(SecurityTxtEnabledKey);
        var enabled = bool.TryParse(enabledRaw, out var e) && e;

        var expiresRaw = Get(SecurityTxtExpiresKey);
        DateTimeOffset? expires = DateTimeOffset.TryParseExact(expiresRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var exp) ? exp : null;

        return new SecurityTxtSettings(
            Enabled: enabled,
            Contact: Get(SecurityTxtContactKey),
            Expires: expires,
            Encryption: Get(SecurityTxtEncryptionKey),
            Acknowledgments: Get(SecurityTxtAcknowledgmentsKey),
            PreferredLanguages: Get(SecurityTxtPreferredLanguagesKey),
            Canonical: Get(SecurityTxtCanonicalKey),
            Policy: Get(SecurityTxtPolicyKey),
            Hiring: Get(SecurityTxtHiringKey));
    }

    public async Task SetSecurityTxtSettingsAsync(SecurityTxtSettings settings, CancellationToken ct = default)
    {
        var existing = await _db.Set<AppSetting>()
            .Where(s => SecurityTxtKeys.Contains(s.Key))
            .ToListAsync(ct);

        void Upsert(string key, string? value)
        {
            var kv = existing.FirstOrDefault(r => r.Key == key);
            if (value == null)
            {
                if (kv != null) _db.Remove(kv);
                return;
            }
            if (kv == null)
                _db.Add(new AppSetting { Key = key, Value = value });
            else
                kv.Value = value;
        }

        Upsert(SecurityTxtEnabledKey, settings.Enabled.ToString());
        Upsert(SecurityTxtContactKey, settings.Contact);
        Upsert(SecurityTxtExpiresKey, settings.Expires?.ToString("O"));
        Upsert(SecurityTxtEncryptionKey, settings.Encryption);
        Upsert(SecurityTxtAcknowledgmentsKey, settings.Acknowledgments);
        Upsert(SecurityTxtPreferredLanguagesKey, settings.PreferredLanguages);
        Upsert(SecurityTxtCanonicalKey, settings.Canonical);
        Upsert(SecurityTxtPolicyKey, settings.Policy);
        Upsert(SecurityTxtHiringKey, settings.Hiring);

        await _db.SaveChangesAsync(ct);
    }

    private async Task WriteString(string key, string value, CancellationToken ct)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { key }, ct);
        if (kv == null)
        {
            _db.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            kv.Value = value;
            _db.Update(kv);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task WriteBool(string key, bool value, CancellationToken ct)
    {
        await WriteString(key, value.ToString(), ct);
    }

    private async Task WriteNullableInt(string key, int? value, CancellationToken ct)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { key }, ct);
        if (value == null)
        {
            if (kv != null) { _db.Remove(kv); await _db.SaveChangesAsync(ct); }
            return;
        }
        if (kv == null)
        {
            _db.Add(new AppSetting { Key = key, Value = value.Value.ToString() });
        }
        else
        {
            kv.Value = value.Value.ToString();
            _db.Update(kv);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpdateUserSettingAsync(string userId, Action<UserSetting> update, CancellationToken ct)
    {
        var set = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        if (set == null)
        {
            set = new UserSetting { UserId = userId };
            update(set);
            _db.Add(set);
        }
        else
        {
            update(set);
            _db.Update(set);
        }
        await _db.SaveChangesAsync(ct);
    }
}
