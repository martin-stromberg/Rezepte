using Microsoft.AspNetCore.DataProtection;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class DataProtectionSystemSecretStore(RezepteDbContext db, IDataProtectionProvider dataProtectionProvider) : ISystemSecretStore
{
    private const string Prefix = "plugin-secret:";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Rezepte.PluginSources.Pat");

    public async Task StoreAsync(string name, string secret, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name is required.", nameof(name));
        }

        var key = Prefix + name;
        var setting = await db.AppSettings.FindAsync([key], ct).ConfigureAwait(false);
        var protectedValue = _protector.Protect(secret);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = protectedValue });
        }
        else
        {
            setting.Value = protectedValue;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<string?> GetAsync(string name, CancellationToken ct = default)
    {
        var setting = await db.AppSettings.FindAsync([Prefix + name], ct).ConfigureAwait(false);
        return setting is null ? null : _protector.Unprotect(setting.Value);
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        var setting = await db.AppSettings.FindAsync([Prefix + name], ct).ConfigureAwait(false);
        if (setting is null)
        {
            return;
        }

        db.AppSettings.Remove(setting);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
