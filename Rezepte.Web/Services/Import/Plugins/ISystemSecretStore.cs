namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the isystem secret store interface.
/// </summary>
public interface ISystemSecretStore
{
    /// <summary>
    /// stores the async.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <param name="secret">The secret parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task StoreAsync(string name, string secret, CancellationToken ct = default);
    /// <summary>
    /// Gets the async.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<string?> GetAsync(string name, CancellationToken ct = default);
    /// <summary>
    /// Deletes the async.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task DeleteAsync(string name, CancellationToken ct = default);
}
