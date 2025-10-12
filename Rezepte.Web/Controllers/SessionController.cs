using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
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

        // Issue API token (JWT) and cache by user
        tokens.CreateToken(user.Id, user.Username);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("logout")] // clears auth cookie
    public async Task<IActionResult> Logout([FromServices] ITokenService tokens, [FromQuery] string? returnUrl)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/login" : returnUrl);
    }

    public record LoginDto(string Username, string Password, bool RememberMe);
}
