using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Controllers
{
    [ApiController]
    [Route("api/import")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ImportTestController : ControllerBase
    {
        private readonly ITestRecipeImportService _testService;
        private readonly IRecipeService _recipes;
        private readonly ILogger<ImportTestController> _logger;

        public ImportTestController(ITestRecipeImportService testService, IRecipeService recipes, ILogger<ImportTestController> logger)
        {
            _testService = testService;
            _recipes = recipes;
            _logger = logger;
        }

        [HttpGet("test-urls")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTestUrls(CancellationToken ct)
        {
            var urls = await _testService.GetTestUrlsAsync(ct);
            return Ok(urls);
        }

        public record DeleteByUrlRequest(string Url);

        [HttpPost("test-delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteByUrl([FromBody] DeleteByUrlRequest req, CancellationToken ct)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Url)) return BadRequest("Url required");

            // Best effort UserId-Ermittlung (Controller läuft üblicherweise mit Auth)
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Löschen per Testlauf angefragt, aber kein User kontext vorhanden.");
                return Unauthorized("No user context available");
            }

            var existing = await _recipes.FindByUri(userId, req.Url, ct);
            if (existing == null)
                existing = await _recipes.FindByUri(userId, $"{req.Url}/", ct);
            if (existing == null) return Ok(new { deleted = false });

            var (ok, error) = await _recipes.DeleteAsync(userId, existing.Id, ct);
            if (!ok) return StatusCode(500, new { deleted = false, error });

            return Ok(new { deleted = true });
        }
    }
}