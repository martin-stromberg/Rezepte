using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/admin/exports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminExportsController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<AdminExportsController> _logger;

    public AdminExportsController(IExportService exportService, ILogger<AdminExportsController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Admin-Export: Exportiert alle Daten (inkl. Benutzer).
    /// POST /api/admin/exports?includePdf=true
    /// Liefert ZIP-Stream zurück.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportAll([FromQuery] bool includeImages = false, [FromQuery] bool includePdf = false, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        _logger.LogInformation("Admin {AdminId} started full export includePdf={IncludePdf}", adminId, includePdf);

        Stream zipStream;
        try
        {
            zipStream = await _exportService.ExportAllAsync(adminId, includeImages, includePdf, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin export failed (admin={AdminId})", adminId);
            return Problem(title: "Admin export failed", detail: ex.Message, statusCode: 500);
        }

        zipStream.Seek(0, SeekOrigin.Begin);
        var fileName = $"export-all-{DateTime.UtcNow:yyyyMMddHHmm}.zip";
        return File(zipStream, "application/zip", fileName);
    }

    /// <summary>
    /// Admin-Import: Stellt Daten aus einer ZIP-Datei wieder her.
    /// POST /api/admin/exports/restore
    /// </summary>
    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromForm] IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId)) return Unauthorized();

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

            // Delegiere die eigentliche Wiederherstellungslogik an den Service.
            // Implementierung ist vorsichtig: legt nur fehlende Entitäten an, überschreibt nichts.
            await _exportService.RestoreFromZipAsync(ms, adminId, ct).ConfigureAwait(false);

            _logger.LogInformation("Admin {AdminId} uploaded restore file ({Size} bytes) and restore was started.", adminId, file.Length);
            // Rückgabe 200 OK oder 202 Accepted je nach Implementationsentscheid (hier: synchron ausgeführt -> OK)
            return Ok("Restore completed.");
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed (admin={AdminId})", adminId);
            return Problem(title: "Restore failed", detail: ex.Message, statusCode: 500);
        }
    }
}