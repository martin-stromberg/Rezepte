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

    public SettingsController(ISettingsService settings)
    {
        _settings = settings;
    }

    // GET api/settings/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMySettings(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var aiEnabled = await _settings.GetUserAiEnabledAsync(userId, ct);
        var global = await _settings.GetGlobalAiEnabledAsync(ct);
        return Ok(new { AiEnabled = aiEnabled, GlobalAiEnabled = global });
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

    // Admin: GET global
    [HttpGet("global")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> GetGlobal(CancellationToken ct)
    {
        var global = await _settings.GetGlobalAiEnabledAsync(ct);
        return Ok(new { GlobalAiEnabled = global });
    }

    // Admin: set global
    [HttpPut("global/ai")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetGlobalAi([FromBody] bool enabled, CancellationToken ct)
    {
        await _settings.SetGlobalAiEnabledAsync(enabled, ct);
        return NoContent();
    }
}