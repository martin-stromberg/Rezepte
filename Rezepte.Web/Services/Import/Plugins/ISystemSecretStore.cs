namespace Rezepte.Web.Services.Import.Plugins;

public interface ISystemSecretStore
{
    Task StoreAsync(string name, string secret, CancellationToken ct = default);
    Task<string?> GetAsync(string name, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
}
