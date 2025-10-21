namespace Rezepte.Web.Services;

public interface ISettingsService
{
    Task<bool> GetUserAiEnabledAsync(string userId, CancellationToken ct = default);
    Task SetUserAiEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    Task<bool> GetGlobalAiEnabledAsync(CancellationToken ct = default);
    Task SetGlobalAiEnabledAsync(bool enabled, CancellationToken ct = default);

    // per-user service toggles
    Task<bool> GetUserGoogleVisionEnabledAsync(string userId, CancellationToken ct = default);
    Task SetUserGoogleVisionEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    Task<bool> GetUserGeminiEnabledAsync(string userId, CancellationToken ct = default);
    Task SetUserGeminiEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    // global per-service toggles
    Task<bool> GetGlobalGoogleVisionEnabledAsync(CancellationToken ct = default);
    Task SetGlobalGoogleVisionEnabledAsync(bool enabled, CancellationToken ct = default);

    Task<bool> GetGlobalGeminiEnabledAsync(CancellationToken ct = default);
    Task SetGlobalGeminiEnabledAsync(bool enabled, CancellationToken ct = default);

    // New: per-user confirmation requirement
    Task<bool> GetUserRequireAiConfirmationAsync(string userId, CancellationToken ct = default);
    Task SetUserRequireAiConfirmationAsync(string userId, bool required, CancellationToken ct = default);
}