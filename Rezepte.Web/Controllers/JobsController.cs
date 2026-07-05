using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class JobsController : ControllerBase
{
    private readonly IBackgroundJobQueue _queue;
    private readonly ExportJobFileStore _fileStore;

    public JobsController(IBackgroundJobQueue queue, ExportJobFileStore fileStore)
    {
        _queue = queue;
        _fileStore = fileStore;
    }

    private string? GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Enqueue a user export job. Returns job id.
    /// POST /api/jobs/exports/me
    /// Body (optional): { "includePdf": true }
    /// </summary>
    [HttpPost("exports/me")]
    public async Task<IActionResult> EnqueueUserExport([FromBody] JsonElement? body = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var payloadJson = CreatePayloadJson(body);
        // Enqueue job: jobType "export:user", initiatorUserId set
        var jobId = await _queue.EnqueueAsync("export:user", payloadJson, userId, ct).ConfigureAwait(false);
        return Accepted(new
        {
            jobId,
            statusUrl = Url.ActionLink(nameof(GetJobStatus), values: new { id = jobId }),
            downloadUrl = Url.ActionLink(nameof(DownloadJobResult), values: new { id = jobId })
        });
    }

    /// <summary>
    /// Enqueue an admin export job. Returns job id.
    /// POST /api/jobs/exports/all
    /// Body (optional): { "includeImages": true, "includePdf": true }
    /// </summary>
    [HttpPost("exports/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EnqueueAdminExport([FromBody] JsonElement? body = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var payloadJson = CreatePayloadJson(body, defaultIncludeImages: true);
        var jobId = await _queue.EnqueueAsync("export:all", payloadJson, userId, ct).ConfigureAwait(false);
        return Accepted(new
        {
            jobId,
            statusUrl = Url.ActionLink(nameof(GetJobStatus), values: new { id = jobId }),
            downloadUrl = Url.ActionLink(nameof(DownloadJobResult), values: new { id = jobId })
        });
    }

    /// <summary>
    /// Query job status.
    /// GET /api/jobs/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobStatus(Guid id, CancellationToken ct = default)
    {
        var job = await _queue.GetJobAsync(id, ct).ConfigureAwait(false);
        if (job == null) return NotFound();
        if (!CanAccessJob(job)) return Forbid();

        var downloadUrl = job.Status == BackgroundJobStatus.Succeeded && IsExportJob(job)
            ? Url.ActionLink(nameof(DownloadJobResult), values: new { id = job.Id })
            : null;

        return Ok(new
        {
            job.Id,
            job.JobType,
            job.Status,
            job.Progress,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.ResultMessage,
            job.Error,
            DownloadUrl = downloadUrl
        });
    }

    /// <summary>
    /// Download the result file of a finished export job.
    /// GET /api/jobs/{id}/download
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadJobResult(Guid id, CancellationToken ct = default)
    {
        var job = await _queue.GetJobAsync(id, ct).ConfigureAwait(false);
        if (job == null) return NotFound();
        if (!CanAccessJob(job)) return Forbid();
        if (!IsExportJob(job)) return BadRequest(new ProblemDetails { Title = "Job has no downloadable export." });
        if (job.Status != BackgroundJobStatus.Succeeded) return Conflict(new ProblemDetails { Title = "Export is not finished yet." });
        if (string.IsNullOrWhiteSpace(job.ResultMessage)) return NotFound();

        var filePath = _fileStore.GetPathForFileName(job.ResultMessage);
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var timestamp = job.CompletedAt?.ToString("yyyyMMddHHmm") ?? DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var fileName = string.Equals(job.JobType, "export:all", StringComparison.OrdinalIgnoreCase)
            ? $"export-all-{timestamp}.zip"
            : $"recipes-{job.InitiatorUserId}-{timestamp}.zip";

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/zip", fileName);
    }

    private static string CreatePayloadJson(JsonElement? body, bool defaultIncludeImages = false)
    {
        if (!body.HasValue)
        {
            return new ExportJobPayload(defaultIncludeImages, IncludePdf: false).ToJson();
        }

        try
        {
            var parsed = ExportJobPayload.FromJson(body.Value.GetRawText());
            return (parsed with { IncludeImages = parsed.IncludeImages || defaultIncludeImages }).ToJson();
        }
        catch (JsonException)
        {
            return new ExportJobPayload(defaultIncludeImages, IncludePdf: false).ToJson();
        }
    }

    private bool CanAccessJob(BackgroundJob job)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return false;
        return string.Equals(job.InitiatorUserId, userId, StringComparison.Ordinal) || User.IsInRole("Admin");
    }

    private static bool IsExportJob(BackgroundJob job)
    {
        return string.Equals(job.JobType, "export:user", StringComparison.OrdinalIgnoreCase)
            || string.Equals(job.JobType, "export:all", StringComparison.OrdinalIgnoreCase);
    }
}
