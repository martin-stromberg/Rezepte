using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Contracts;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Authentication endpoints such as registration. Accepts form posts for the website and JSON for API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Registers a new user from form or JSON payloads. On form post, redirects to /login; otherwise returns JSON response.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirect to /login on form posts or 200 OK with <see cref="AuthResponse"/> for JSON.</returns>
    [IgnoreAntiforgeryToken]
    [HttpPost("register")]
    public async Task<IActionResult> Register(CancellationToken ct)
    {
        string? username = null;
        string? password = null;
        string? email = null;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            username = form["Username"].FirstOrDefault();
            password = form["Password"].FirstOrDefault();
            email = form["Email"].FirstOrDefault();
        }
        else
        {
            var dto = await Request.ReadFromJsonAsync<RegisterRequest>(cancellationToken: ct);
            if (dto is not null)
            {
                username = dto.Username;
                password = dto.Password;
                email = dto.Email;
            }
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            if (Request.HasFormContentType)
            {
                return LocalRedirect("/register?error=1");
            }
            return BadRequest(new { message = "Username and Password are required" });
        }

        var (ok, error, user) = await _userService.RegisterAsync(username, password, ct);
        if (!ok || user is null)
        {
            if (Request.HasFormContentType)
            {
                return LocalRedirect("/register?error=1");
            }
            return BadRequest(new { message = error ?? "Registration failed" });
        }

        // When form post: redirect to login instead of returning JSON
        if (Request.HasFormContentType)
        {
            return LocalRedirect("/login");
        }

        return Ok(new AuthResponse(user.Id, user.Username, user.Email));
    }

    /// <summary>Form DTO used by the website registration page.</summary>
    public record RegisterRequestForm(string? Email, string Username, string Password);
}
