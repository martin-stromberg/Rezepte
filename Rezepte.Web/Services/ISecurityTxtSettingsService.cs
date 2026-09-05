using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

/// <summary>
/// Defines the isecurity txt settings service interface.
/// </summary>
public interface ISecurityTxtSettingsService
{
    /// <summary>
    /// Gets the security txt settings async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<SecurityTxtSettings> GetSecurityTxtSettingsAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the security txt settings async.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetSecurityTxtSettingsAsync(SecurityTxtSettings settings, CancellationToken ct = default);
}
