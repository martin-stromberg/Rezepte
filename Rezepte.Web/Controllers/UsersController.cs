using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Contracts;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// User profile endpoints for the authenticated user (JWT protected).
/// </summary>
/// <param name="users">The users parameter.</param>
/// <param>...</param>
/// <param>...</param>
/// <param>...</param>
/// <returns>The result.</returns>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController(IUserService users) : ControllerBase
{
    private readonly IUserService _users = users;

    /// <summary>
    /// Returns the profile of the current user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="UserProfileDto"/> or 401/404.</returns>
    [IgnoreAntiforgeryToken]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return NotFound();

        return new UserProfileDto(user.Id, user.Username, user.Email);
    }

    /// <summary>
    /// Updates the current user's profile (username and e-mail).
    /// </summary>
    /// <param name="dto">Profile update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated profile or 400/401.</returns>
    [IgnoreAntiforgeryToken]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest(new { message = "Ungültige Eingaben." });

        var (ok, error, updated) = await _users.UpdateProfileAsync(userId, dto.Username, dto.Email, ct);
        if (!ok || updated is null)
            return BadRequest(new { message = error ?? "Profil konnte nicht aktualisiert werden." });

        return Ok(new UserProfileDto(updated.Id, updated.Username, updated.Email));
    }

    /// <summary>
    /// Changes the password of the current user.
    /// </summary>
    /// <param name="dto">Password change request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK or 400/401 with message.</returns>
    [IgnoreAntiforgeryToken]
    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest(new { message = "Ungültige Eingaben." });

        var (ok, error) = await _users.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Passwort konnte nicht geändert werden." });

        return Ok();
    }
}
