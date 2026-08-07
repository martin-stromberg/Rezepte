using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Extensions;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/admin/exports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminExportsController : ApiControllerBase
{
    private readonly IExportService _exportService;
    private readonly IBackgroundJobQueue _jobQueue;
    private readonly ILogger<AdminExportsController> _logger;

    public AdminExportsController(IExportService exportService, IBackgroundJobQueue jobQueue, ILogger<AdminExportsController> logger)
    {
        _exportService = exportService;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    /// <summary>
    /// Admin-Export: Exportiert alle Daten (inkl. Benutzer).
    /// POST /api/admin/exports?includePdf=true
    /// Startet einen Hintergrundjob und liefert dessen Job-ID zurueck.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportAll([FromQuery] bool includeImages = false, [FromQuery] bool includePdf = false, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        _logger.LogInformation("Admin {AdminId} queued full export includePdf={IncludePdf} includeImages={IncludeImages}", adminId, includePdf, includeImages);

        try
        {
            var payloadJson = new ExportJobPayload(includeImages, includePdf).ToJson();
            var jobId = await _jobQueue.EnqueueAsync("export:all", payloadJson, adminId, ct).ConfigureAwait(false);
            return Accepted(new
            {
                jobId,
                statusUrl = Url.ActionLink("GetJobStatus", "Jobs", new { id = jobId }),
                downloadUrl = Url.ActionLink("DownloadJobResult", "Jobs", new { id = jobId })
            });
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
    }

    /// <summary>
    /// Admin-Import: Stellt Daten aus einer ZIP-Datei wieder her.
    /// POST /api/admin/exports/restore
    /// </summary>
    [HttpPost("restore")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(524288000)] // 500 MB limit, anpassen nach Bedarf
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> Restore([FromForm(Name = "file")] IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Missing file", Detail = "Form field 'file' is required." });
        }

        var adminId = GetUserId();
        if (string.IsNullOrEmpty(adminId)) return Unauthorized();

        try
        {
            await using var ms = await file.ReadToMemoryStreamAsync(ct);

            // Delegiere die eigentliche Wiederherstellungslogik an den Service.
            // Implementierung ist vorsichtig: legt nur fehlende Entitaeten an, ueberschreibt nichts.
            await _exportService.RestoreFromZipAsync(ms, adminId, ct).ConfigureAwait(false);

            _logger.LogInformation("Admin {AdminId} uploaded restore file ({Size} bytes) and restore was started.", adminId, file.Length);
            // Rueckgabe 200 OK oder 202 Accepted je nach Implementationsentscheid (hier: synchron ausgefuehrt -> OK)
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
