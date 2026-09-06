using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

/// <summary>
/// Defines the isettings service interface.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the user ai enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetUserAiEnabledAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Sets the user ai enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetUserAiEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Gets the global ai enabled async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetGlobalAiEnabledAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the global ai enabled async.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetGlobalAiEnabledAsync(bool enabled, CancellationToken ct = default);

    // per-user service toggles
    /// <summary>
    /// Gets the user google vision enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetUserGoogleVisionEnabledAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Sets the user google vision enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetUserGoogleVisionEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Gets the user gemini enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetUserGeminiEnabledAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Sets the user gemini enabled async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetUserGeminiEnabledAsync(string userId, bool enabled, CancellationToken ct = default);

    // global per-service toggles
    /// <summary>
    /// Gets the global google vision enabled async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetGlobalGoogleVisionEnabledAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the global google vision enabled async.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetGlobalGoogleVisionEnabledAsync(bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Gets the global gemini enabled async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetGlobalGeminiEnabledAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the global gemini enabled async.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetGlobalGeminiEnabledAsync(bool enabled, CancellationToken ct = default);

    // New: per-user confirmation requirement
    /// <summary>
    /// Gets the user require ai confirmation async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetUserRequireAiConfirmationAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Sets the user require ai confirmation async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="required">The required parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetUserRequireAiConfirmationAsync(string userId, bool required, CancellationToken ct = default);

    // per-user shopping list display mode
    /// <summary>
    /// Gets the user shopping list edit mode async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetUserShoppingListEditModeAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Sets the user shopping list edit mode async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="editMode">The edit mode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetUserShoppingListEditModeAsync(string userId, bool editMode, CancellationToken ct = default);

    // New: global limits and behaviour
    /// <summary>
    /// Gets the global max requests per hour async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<int?> GetGlobalMaxRequestsPerHourAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the global max requests per hour async.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetGlobalMaxRequestsPerHourAsync(int? value, CancellationToken ct = default);

    /// <summary>
    /// Gets the global max requests per day async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<int?> GetGlobalMaxRequestsPerDayAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the global max requests per day async.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetGlobalMaxRequestsPerDayAsync(int? value, CancellationToken ct = default);

    /// <summary>
    /// Gets the global disable on limit reached async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<bool> GetGlobalDisableOnLimitReachedAsync(CancellationToken ct = default);
    /// <summary>
    /// Sets the global disable on limit reached async.
    /// </summary>
    /// <param name="disable">The disable parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    Task SetGlobalDisableOnLimitReachedAsync(bool disable, CancellationToken ct = default);

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
