using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Administration endpoints for managing user accounts (JWT + Admin role required).
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminUsersController(IUserService users) : ControllerBase
{
    private readonly IUserService _users = users;

    /// <summary>
    /// Returns all users for administration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await _users.GetAllAsync(ct);
        var result = list.Select(u => new { u.Id, u.Username, u.Email, u.IsAdmin }).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="dto">Creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest dto, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (!dto.Email.Contains('@') || dto.Email.Length > 256)
                return BadRequest(new { message = "Die E-Mail ist ungültig." });
        }
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return BadRequest(new { message = "Das Passwort muss mindestens 6 Zeichen haben." });

        var (ok, error, user) = await _users.RegisterAsync(dto.Username, dto.Password, ct);
        if (!ok || user is null)
            return BadRequest(new { message = error ?? "Anlegen fehlgeschlagen." });

        // Optionally set admin flag
        if (dto.IsAdmin && !user.IsAdmin)
        {
            await _users.UpdateUserAsync(user.Id, user.Username, user.Email, true, ct);
            user = user with { IsAdmin = true };
        }

        return Ok(new { user.Id, user.Username, user.Email, user.IsAdmin });
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="dto">Update request.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest dto, CancellationToken ct)
    {
        var (ok, error) = await _users.UpdateUserAsync(id, dto.Username, dto.Email, dto.IsAdmin, ct);
        if (!ok) return BadRequest(new { message = error ?? "Aktualisieren fehlgeschlagen." });
        return NoContent();
    }

    /// <summary>
    /// Deletes a user by id.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (ok, error) = await _users.DeleteAsync(id, ct);
        if (!ok) return BadRequest(new { message = error ?? "Delete failed" });
        return NoContent();
    }

    /// <summary>Creation DTO.</summary>
    public record CreateUserRequest(string Username, string? Email, string Password, bool IsAdmin);

    /// <summary>Update DTO.</summary>
    public record UpdateUserRequest(string Username, string? Email, bool IsAdmin);
}
