using Microsoft.AspNetCore.DataProtection;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// datas the protection system secret store.
/// </summary>
/// <param name="db">The db parameter.</param>
/// <param name="dataProtectionProvider">The data protection provider parameter.</param>
/// <returns>The result.</returns>
public sealed class DataProtectionSystemSecretStore(RezepteDbContext db, IDataProtectionProvider dataProtectionProvider) : ISystemSecretStore
{
    private const string Prefix = "plugin-secret:";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Rezepte.PluginSources.Pat");

    /// <summary>
    /// stores the async.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <param name="secret">The secret parameter.</param>
    /// <param name="ct">The ct parameter.</param>
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

    /// <summary>
    /// Gets the async.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<string?> GetAsync(string name, CancellationToken ct = default)
    {
        var setting = await db.AppSettings.FindAsync([Prefix + name], ct).ConfigureAwait(false);
        return setting is null ? null : _protector.Unprotect(setting.Value);
    }

    /// <summary>
    /// Deletes the async.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <param name="ct">The ct parameter.</param>
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
