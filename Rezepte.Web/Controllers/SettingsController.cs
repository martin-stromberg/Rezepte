using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;
using System.Security.Claims;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;
    private readonly IGoogleCredentialsProvider _googleCredentialsProvider;

    public SettingsController(ISettingsService settings, IGoogleCredentialsProvider googleCredentialsProvider)
    {
        _settings = settings;
        this._googleCredentialsProvider = googleCredentialsProvider;
    }

    // GET api/settings/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMySettings(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var aiEnabled = await _settings.GetUserAiEnabledAsync(userId, ct);
        var userGoogle = await _settings.GetUserGoogleVisionEnabledAsync(userId, ct);
        var userGemini = await _settings.GetUserGeminiEnabledAsync(userId, ct);
        var requireConfirm = await _settings.GetUserRequireAiConfirmationAsync(userId, ct);

        var global = await _settings.GetGlobalAiEnabledAsync(ct);
        var globalGoogle = await _settings.GetGlobalGoogleVisionEnabledAsync(ct);
        var globalGemini = await _settings.GetGlobalGeminiEnabledAsync(ct);

        var serviceAccountAvailable = _googleCredentialsProvider.ServiceAccountFileExists();
        var apiKeyAvailable = !string.IsNullOrWhiteSpace(_googleCredentialsProvider.GetGeminiApiKey());
        return Ok(new
        {
            GoogleServiceAccountFileAvailable = serviceAccountAvailable,
            GeminiApiKeyAvailable = apiKeyAvailable,
            AiEnabled = aiEnabled,
            UserGoogleVisionEnabled = userGoogle,
            UserGeminiEnabled = userGemini,
            RequireAiConfirmation = requireConfirm,
            GlobalAiEnabled = global,
            GlobalGoogleVisionEnabled = globalGoogle,
            GlobalGeminiEnabled = globalGemini
        });
    }

    // PUT api/settings/me/ai
    [HttpPut("me/ai")]
    public async Task<IActionResult> SetMyAi([FromBody] bool enabled, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _settings.SetUserAiEnabledAsync(userId, enabled, ct);
        return NoContent();
    }

    // PUT api/settings/me/ai/googlevision
    [HttpPut("me/ai/googlevision")]
    public async Task<IActionResult> SetMyGoogleVision([FromBody] bool enabled, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _settings.SetUserGoogleVisionEnabledAsync(userId, enabled, ct);
        return NoContent();
    }

    // PUT api/settings/me/ai/gemini
    [HttpPut("me/ai/gemini")]
    public async Task<IActionResult> SetMyGemini([FromBody] bool enabled, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _settings.SetUserGeminiEnabledAsync(userId, enabled, ct);
        return NoContent();
    }

    // PUT api/settings/me/ai/confirm
    [HttpPut("me/ai/confirm")]
    public async Task<IActionResult> SetMyAiConfirm([FromBody] bool required, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _settings.SetUserRequireAiConfirmationAsync(userId, required, ct);
        return NoContent();
    }

    // Admin: GET global
    [HttpGet("global")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> GetGlobal(CancellationToken ct)
    {
        var global = await _settings.GetGlobalAiEnabledAsync(ct);
        var globalGoogle = await _settings.GetGlobalGoogleVisionEnabledAsync(ct);
        var globalGemini = await _settings.GetGlobalGeminiEnabledAsync(ct);
        return Ok(new { GlobalAiEnabled = global, GlobalGoogleVisionEnabled = globalGoogle, GlobalGeminiEnabled = globalGemini });
    }

    // Admin: set global ai
    [HttpPut("global/ai")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalAi([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalAiEnabledAsync(enabled, ct);
        return NoContent();
    }

    // Admin: set global googlevision
    [HttpPut("global/ai/googlevision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalGoogleVision([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalGoogleVisionEnabledAsync(enabled, ct);
        return NoContent();
    }

    // Admin: set global gemini
    [HttpPut("global/ai/gemini")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalGemini([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalGeminiEnabledAsync(enabled, ct);
        return NoContent();
    }
}