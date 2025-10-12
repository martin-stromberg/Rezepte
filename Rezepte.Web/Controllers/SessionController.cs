using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Session endpoints for login/logout. Issues the website auth cookie and caches a JWT for API calls.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    /// <summary>
    /// Authenticates the user and establishes a session via auth cookie. Also creates a JWT for API requests.
    /// </summary>
    /// <param name="users">User service.</param>
    /// <param name="tokens">Token service.</param>
    /// <param name="dto">Login form values.</param>
    /// <param name="returnUrl">Optional return URL after login.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirects to the specified return URL or to '/login?error=1' on failure.</returns>
    [IgnoreAntiforgeryToken]
    [HttpPost("login")] // issues auth cookie + api token
    public async Task<IActionResult> Login([FromServices] IUserService users, [FromServices] ITokenService tokens, [FromForm] LoginDto dto, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        var user = await users.LoginAsync(dto.Username, dto.Password, ct);
        if (user is null)
        {
            return LocalRedirect("/login?error=1");
        }

        // Website auth cookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username)
        };
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = dto.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        // Issue API token (JWT) and cache by user; include role claim if present
        tokens.CreateToken(user.Id, user.Username, user.IsAdmin);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    /// <summary>
    /// Logs the current user out by clearing the auth cookie (and effectively invalidating future API auth through cache expiration).
    /// </summary>
    /// <param name="tokens">Token service.</param>
    /// <param name="returnUrl">Optional return URL after logout.</param>
    /// <returns>Redirect to '/login' or provided return URL.</returns>
    [IgnoreAntiforgeryToken]
    [HttpPost("logout")] // clears auth cookie
    public async Task<IActionResult> Logout([FromServices] ITokenService tokens, [FromQuery] string? returnUrl)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/login" : returnUrl);
    }

    /// <summary>
    /// Form DTO for login.
    /// </summary>
    /// <param name="Username">Username.</param>
    /// <param name="Password">Password.</param>
    /// <param name="RememberMe">When true, creates a persistent cookie.</param>
    public record LoginDto(string Username, string Password, bool RememberMe);
}
