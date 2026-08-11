using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

public interface ISecurityTxtSettingsService
{
    Task<SecurityTxtSettings> GetSecurityTxtSettingsAsync(CancellationToken ct = default);
    Task SetSecurityTxtSettingsAsync(SecurityTxtSettings settings, CancellationToken ct = default);
}
