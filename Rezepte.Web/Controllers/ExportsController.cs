using System.IO.Compression;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ExportsController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportsController> _logger;

    public ExportsController(IExportService exportService, ILogger<ExportsController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Exportiert die Rezeptesammlung des angemeldeten Benutzers.
    /// Query: ?format=csv|json (default zip), ?includePdf=true|false, ?includeImages=true|false
    /// Liefert eine ZIP-Datei oder die recipes.json als JSON-Datei zurück.
    /// </summary>
    [HttpGet("recipes")]
    public async Task<IActionResult> ExportMyRecipes([FromQuery] string format = "json", [FromQuery] bool includeImages = false, [FromQuery] bool includePdf = false, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        _logger.LogInformation("User {UserId} requested export format={Format} includePdf={IncludePdf} includeImages={includeImages}", userId, format, includePdf, includeImages);

        Stream exportStream;
        try
        {
            exportStream = await _exportService.ExportUserAsync(userId, includeImages, includePdf, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499); // client closed request / cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed for user {UserId}", userId);
            return Problem(title: "Export failed", detail: ex.Message, statusCode: 500);
        }

        try
        {
            var archive = new ZipArchive(exportStream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count == 1)
            {
                var entry = archive.Entries.First();
                if (entry == null)
                    return Problem(title: "Export format error", detail: "recipes.json not found inside archive", statusCode: 500);

                using var entryStream = entry.Open();
                var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms, ct).ConfigureAwait(false);
                ms.Seek(0, SeekOrigin.Begin);
                var fileName = $"recipes-{userId}-{DateTime.UtcNow:yyyyMMddHHmm}.json";
                archive.Dispose();
                return File(ms, "application/json; charset=utf-8", fileName);
            }
            else
            {
                exportStream.Seek(0, SeekOrigin.Begin);
                var fileName = $"recipes-{userId}-{DateTime.UtcNow:yyyyMMddHHmm}.zip";
                return File(exportStream, "application/zip", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract recipes.json for user {UserId}", userId);
            return Problem(title: "Export failed", detail: ex.Message, statusCode: 500);
        }
    }
}