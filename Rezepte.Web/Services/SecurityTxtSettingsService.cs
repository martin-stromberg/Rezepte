using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Dtos;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

public class SecurityTxtSettingsService : ISecurityTxtSettingsService
{
    private readonly RezepteDbContext _db;

    public SecurityTxtSettingsService(RezepteDbContext db)
    {
        _db = db;
    }

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
}
