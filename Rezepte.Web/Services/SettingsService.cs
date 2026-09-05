using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Dtos;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the settings service class.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly RezepteDbContext _db;
    private readonly ISecurityTxtSettingsService _securityTxtSettingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="db">The db parameter.</param>
    /// <param name="securityTxtSettingsService">The security txt settings service parameter.</param>
    public SettingsService(RezepteDbContext db, ISecurityTxtSettingsService securityTxtSettingsService)
    {
        _db = db;
        _securityTxtSettingsService = securityTxtSettingsService;
    }

    /// <summary>
    /// Gets the user ai enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetUserAiEnabledAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return true;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.AiEnabled ?? true;
    }

    /// <summary>
    /// Sets the user ai enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
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

    /// <summary>
    /// Gets the global ai enabled async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetGlobalAiEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { AiKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    /// <summary>
    /// Sets the global ai enabled async.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetGlobalAiEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        await WriteBool(AiKey, enabled, ct);
    }

    // per-user service toggles
    /// <summary>
    /// Gets the user google vision enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetUserGoogleVisionEnabledAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return true;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.GoogleVisionEnabled ?? true;
    }

    /// <summary>
    /// Sets the user google vision enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetUserGoogleVisionEnabledAsync(string userId, bool enabled, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        await UpdateUserSettingAsync(userId, s => s.GoogleVisionEnabled = enabled, ct);
    }

    /// <summary>
    /// Gets the user gemini enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetUserGeminiEnabledAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return true;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.GeminiEnabled ?? true;
    }

    /// <summary>
    /// Sets the user gemini enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetUserGeminiEnabledAsync(string userId, bool enabled, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        await UpdateUserSettingAsync(userId, s => s.GeminiEnabled = enabled, ct);
    }

    // New: per-user confirmation requirement
    /// <summary>
    /// Gets the user require ai confirmation async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetUserRequireAiConfirmationAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var s = await _db.Set<UserSetting>().FindAsync(new object[] { userId }, ct);
        return s?.RequireAiConfirmation ?? false;
    }

    /// <summary>
    /// Sets the user require ai confirmation async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="required">The required parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetUserRequireAiConfirmationAsync(string userId, bool required, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        await UpdateUserSettingAsync(userId, s => s.RequireAiConfirmation = required, ct);
    }

    /// <summary>
    /// Gets the user shopping list edit mode async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetUserShoppingListEditModeAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { ShoppingListEditModeKey(userId) }, ct);
        return kv is not null && bool.TryParse(kv.Value, out var value) && value;
    }

    /// <summary>
    /// Sets the user shopping list edit mode async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="editMode">The edit mode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetUserShoppingListEditModeAsync(string userId, bool editMode, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        await WriteString(ShoppingListEditModeKey(userId), editMode.ToString(), ct);
    }

    private static string ShoppingListEditModeKey(string userId) => ShoppingListEditModePrefix + userId;

    // global per-service toggles
    /// <summary>
    /// Gets the global google vision enabled async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetGlobalGoogleVisionEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GoogleVisionKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    /// <summary>
    /// Sets the global google vision enabled async.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetGlobalGoogleVisionEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        await WriteBool(GoogleVisionKey, enabled, ct);
    }

    /// <summary>
    /// Gets the global gemini enabled async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetGlobalGeminiEnabledAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GeminiKey }, ct);
        if (kv == null) return true;
        return bool.TryParse(kv.Value, out var v) ? v : true;
    }

    /// <summary>
    /// Sets the global gemini enabled async.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetGlobalGeminiEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        await WriteBool(GeminiKey, enabled, ct);
    }

    // New: global limits implementation
    /// <summary>
    /// Gets the global max requests per hour async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<int?> GetGlobalMaxRequestsPerHourAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GlobalMaxPerHourKey }, ct);
        if (kv == null) return null;
        if (int.TryParse(kv.Value, out var v)) return v;
        return null;
    }

    /// <summary>
    /// Sets the global max requests per hour async.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetGlobalMaxRequestsPerHourAsync(int? value, CancellationToken ct = default)
    {
        await WriteNullableInt(GlobalMaxPerHourKey, value, ct);
    }

    /// <summary>
    /// Gets the global max requests per day async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<int?> GetGlobalMaxRequestsPerDayAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GlobalMaxPerDayKey }, ct);
        if (kv == null) return null;
        if (int.TryParse(kv.Value, out var v)) return v;
        return null;
    }

    /// <summary>
    /// Sets the global max requests per day async.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetGlobalMaxRequestsPerDayAsync(int? value, CancellationToken ct = default)
    {
        await WriteNullableInt(GlobalMaxPerDayKey, value, ct);
    }

    /// <summary>
    /// Gets the global disable on limit reached async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<bool> GetGlobalDisableOnLimitReachedAsync(CancellationToken ct = default)
    {
        var kv = await _db.Set<AppSetting>().FindAsync(new object[] { GlobalDisableOnLimitKey }, ct);
        if (kv == null) return false;
        return bool.TryParse(kv.Value, out var v) ? v : false;
    }

    /// <summary>
    /// Sets the global disable on limit reached async.
    /// </summary>
    /// <param name="disable">The disable parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetGlobalDisableOnLimitReachedAsync(bool disable, CancellationToken ct = default)
    {
        await WriteBool(GlobalDisableOnLimitKey, disable, ct);
    }

    /// <summary>
    /// Gets the security txt settings async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<SecurityTxtSettings> GetSecurityTxtSettingsAsync(CancellationToken ct = default)
    {
        return await _securityTxtSettingsService.GetSecurityTxtSettingsAsync(ct);
    }

    /// <summary>
    /// Sets the security txt settings async.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SetSecurityTxtSettingsAsync(SecurityTxtSettings settings, CancellationToken ct = default)
    {
        await _securityTxtSettingsService.SetSecurityTxtSettingsAsync(settings, ct);
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
