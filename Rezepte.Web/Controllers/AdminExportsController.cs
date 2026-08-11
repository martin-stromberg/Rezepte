using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/admin/exports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminExportsController : ControllerBase
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

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
            var tempPath = Path.Combine(Path.GetTempPath(), $"rezepte-restore-{Guid.NewGuid()}.zip");
            try
            {
                using (var uploadStream = file.OpenReadStream())
                {
                    await using var tempFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                    await uploadStream.CopyToAsync(tempFile, ct).ConfigureAwait(false);
                }

                await using (var zipFileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                {
                    await _exportService.RestoreFromZipAsync(zipFileStream, adminId, ct).ConfigureAwait(false);
                }

                _logger.LogInformation("Admin {AdminId} uploaded restore file ({Size} bytes) and restore completed.", adminId, file.Length);
                return Ok("Restore completed.");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
            }
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Restore validation failed (admin={AdminId})", adminId);
            return BadRequest(new ProblemDetails { Title = "Invalid restore archive", Detail = ex.Message });
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
