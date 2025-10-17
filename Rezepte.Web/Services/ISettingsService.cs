namespace Rezepte.Web.Services;

public interface ISettingsService
{
    Task<bool> GetUserAiEnabledAsync(string userId, CancellationToken ct = default);
    Task SetUserAiEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    Task<bool> GetGlobalAiEnabledAsync(CancellationToken ct = default);
    Task SetGlobalAiEnabledAsync(bool enabled, CancellationToken ct = default);
}