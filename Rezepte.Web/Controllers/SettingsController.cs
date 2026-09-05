using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Represents the settings controller class.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SettingsController : ApiControllerBase
{
    private readonly ISettingsService _settings;
    private readonly IGoogleCredentialsProvider _googleCredentialsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsController"/> class.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="googleCredentialsProvider">The google credentials provider parameter.</param>
    public SettingsController(ISettingsService settings, IGoogleCredentialsProvider googleCredentialsProvider)
    {
        _settings = settings;
        this._googleCredentialsProvider = googleCredentialsProvider;
    }

    // GET api/settings/me
    /// <summary>
    /// Gets the my settings.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMySettings(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var aiEnabledTask = _settings.GetUserAiEnabledAsync(userId, ct);
        var userGoogleTask = _settings.GetUserGoogleVisionEnabledAsync(userId, ct);
        var userGeminiTask = _settings.GetUserGeminiEnabledAsync(userId, ct);
        var requireConfirmTask = _settings.GetUserRequireAiConfirmationAsync(userId, ct);
        var globalTask = _settings.GetGlobalAiEnabledAsync(ct);
        var globalGoogleTask = _settings.GetGlobalGoogleVisionEnabledAsync(ct);
        var globalGeminiTask = _settings.GetGlobalGeminiEnabledAsync(ct);
        var globalMaxHourTask = _settings.GetGlobalMaxRequestsPerHourAsync(ct);
        var globalMaxDayTask = _settings.GetGlobalMaxRequestsPerDayAsync(ct);
        var globalDisableOnLimitTask = _settings.GetGlobalDisableOnLimitReachedAsync(ct);

        await Task.WhenAll(aiEnabledTask, userGoogleTask, userGeminiTask, requireConfirmTask,
            globalTask, globalGoogleTask, globalGeminiTask, globalMaxHourTask, globalMaxDayTask, globalDisableOnLimitTask);

        var serviceAccountAvailable = _googleCredentialsProvider.ServiceAccountFileExists();
        var apiKeyAvailable = !string.IsNullOrWhiteSpace(_googleCredentialsProvider.GetGeminiApiKey());
        return Ok(new
        {
            GoogleServiceAccountFileAvailable = serviceAccountAvailable,
            GeminiApiKeyAvailable = apiKeyAvailable,
            AiEnabled = aiEnabledTask.Result,
            UserGoogleVisionEnabled = userGoogleTask.Result,
            UserGeminiEnabled = userGeminiTask.Result,
            RequireAiConfirmation = requireConfirmTask.Result,
            GlobalAiEnabled = globalTask.Result,
            GlobalGoogleVisionEnabled = globalGoogleTask.Result,
            GlobalGeminiEnabled = globalGeminiTask.Result,
            GlobalMaxRequestsPerHour = globalMaxHourTask.Result,
            GlobalMaxRequestsPerDay = globalMaxDayTask.Result,
            GlobalDisableOnLimitReached = globalDisableOnLimitTask.Result
        });
    }

    // PUT api/settings/me/ai
    /// <summary>
    /// Sets the my ai.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("me/ai")]
    public async Task<IActionResult> SetMyAi([FromBody] bool enabled, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _settings.SetUserAiEnabledAsync(userId, enabled, ct);
        return NoContent();
    }

    // PUT api/settings/me/ai/googlevision
    /// <summary>
    /// Sets the my google vision.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("me/ai/googlevision")]
    public async Task<IActionResult> SetMyGoogleVision([FromBody] bool enabled, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _settings.SetUserGoogleVisionEnabledAsync(userId, enabled, ct);
        return NoContent();
    }

    // PUT api/settings/me/ai/gemini
    /// <summary>
    /// Sets the my gemini.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("me/ai/gemini")]
    public async Task<IActionResult> SetMyGemini([FromBody] bool enabled, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _settings.SetUserGeminiEnabledAsync(userId, enabled, ct);
        return NoContent();
    }

    // PUT api/settings/me/ai/confirm
    /// <summary>
    /// Sets the my ai confirm.
    /// </summary>
    /// <param name="required">The required parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("me/ai/confirm")]
    public async Task<IActionResult> SetMyAiConfirm([FromBody] bool required, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _settings.SetUserRequireAiConfirmationAsync(userId, required, ct);
        return NoContent();
    }

    // Admin: GET global
    /// <summary>
    /// Gets the global.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("global")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetGlobal(CancellationToken ct)
    {
        var globalTask = _settings.GetGlobalAiEnabledAsync(ct);
        var globalGoogleTask = _settings.GetGlobalGoogleVisionEnabledAsync(ct);
        var globalGeminiTask = _settings.GetGlobalGeminiEnabledAsync(ct);
        var globalMaxHourTask = _settings.GetGlobalMaxRequestsPerHourAsync(ct);
        var globalMaxDayTask = _settings.GetGlobalMaxRequestsPerDayAsync(ct);
        var globalDisableOnLimitTask = _settings.GetGlobalDisableOnLimitReachedAsync(ct);

        await Task.WhenAll(globalTask, globalGoogleTask, globalGeminiTask, globalMaxHourTask, globalMaxDayTask, globalDisableOnLimitTask);

        return Ok(new
        {
            GlobalAiEnabled = globalTask.Result,
            GlobalGoogleVisionEnabled = globalGoogleTask.Result,
            GlobalGeminiEnabled = globalGeminiTask.Result,
            GlobalMaxRequestsPerHour = globalMaxHourTask.Result,
            GlobalMaxRequestsPerDay = globalMaxDayTask.Result,
            GlobalDisableOnLimitReached = globalDisableOnLimitTask.Result
        });
    }

    // Admin: set global ai
    /// <summary>
    /// Sets the global ai.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/ai")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalAi([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalAiEnabledAsync(enabled, ct);
        return NoContent();
    }

    // Admin: set global googlevision
    /// <summary>
    /// Sets the global google vision.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/ai/googlevision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalGoogleVision([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalGoogleVisionEnabledAsync(enabled, ct);
        return NoContent();
    }

    // Admin: set global gemini
    /// <summary>
    /// Sets the global gemini.
    /// </summary>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/ai/gemini")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalGemini([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalGeminiEnabledAsync(enabled, ct);
        return NoContent();
    }

    // Admin: set max requests per hour (nullable = unlimited)
    /// <summary>
    /// Sets the global max requests per hour.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/ai/maxrequestsperhour")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalMaxRequestsPerHour([FromBody] int? value, CancellationToken ct)
    {
        if (value.HasValue && value.Value <= 0) return BadRequest("Der Wert muss größer als 0 sein.");
        await _settings.SetGlobalMaxRequestsPerHourAsync(value, ct);
        return NoContent();
    }

    // Admin: set max requests per day (nullable = unlimited)
    /// <summary>
    /// Sets the global max requests per day.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/ai/maxrequestsperday")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalMaxRequestsPerDay([FromBody] int? value, CancellationToken ct)
    {
        if (value.HasValue && value.Value <= 0) return BadRequest("Der Wert muss größer als 0 sein.");
        await _settings.SetGlobalMaxRequestsPerDayAsync(value, ct);
        return NoContent();
    }

    // Admin: set disable-on-limit flag
    /// <summary>
    /// Sets the global disable on limit.
    /// </summary>
    /// <param name="disable">The disable parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/ai/disableonlimit")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalDisableOnLimit([FromBody] bool disable, CancellationToken ct)
    {
        await _settings.SetGlobalDisableOnLimitReachedAsync(disable, ct);
        return NoContent();
    }

    // Admin: GET security.txt settings
    /// <summary>
    /// Gets the global security txt.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("global/securitytxt")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetGlobalSecurityTxt(CancellationToken ct)
    {
        var settings = await _settings.GetSecurityTxtSettingsAsync(ct);
        return Ok(settings);
    }

    // Admin: PUT security.txt settings
    /// <summary>
    /// Sets the global security txt.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("global/securitytxt")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalSecurityTxt([FromBody] SecurityTxtSettings settings, CancellationToken ct)
    {
        if (settings == null)
            return BadRequest("Einstellungen dürfen nicht null sein.");

        if (settings.Enabled)
        {
            if (string.IsNullOrWhiteSpace(settings.Contact))
                return BadRequest("Contact ist ein Pflichtfeld, wenn security.txt aktiviert ist.");
            if (settings.Expires == null)
                return BadRequest("Expires ist ein Pflichtfeld, wenn security.txt aktiviert ist.");
            if (settings.Expires <= DateTimeOffset.UtcNow)
                return BadRequest("Expires muss in der Zukunft liegen.");
        }

        await _settings.SetSecurityTxtSettingsAsync(settings, ct);
        return NoContent();
    }
}
