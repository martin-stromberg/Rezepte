using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers
{
    [ApiController]
    [Route("api/session")]
    public class SessionTokenController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public SessionTokenController(ITokenService tokenService)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        // Liefert ein kurzlebiges JWT für clientseitige Uploads; geschützt über Cookie-Auth
        [HttpGet("token")]
        [Authorize]
        public IActionResult GetToken()
        {
            var user = HttpContext.User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? user.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var username = user.Identity?.Name ?? userId;
            var isAdmin = user.IsInRole("Admin");

            var token = _tokenService.CreateToken(userId, username, isAdmin);
            return Ok(new { token });
        }
    }
}