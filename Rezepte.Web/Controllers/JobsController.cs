using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IBackgroundJobQueue _queue;

    public JobsController(IBackgroundJobQueue queue)
    {
        _queue = queue;
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

        string? payloadJson = null;
        if (body.HasValue)
        {
            payloadJson = body.Value.GetRawText();
        }
        // Enqueue job: jobType "export:user", initiatorUserId set
        var jobId = await _queue.EnqueueAsync("export:user", payloadJson, userId, ct).ConfigureAwait(false);
        return Accepted(new { jobId });
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
        return Ok(new
        {
            job.Id,
            job.JobType,
            job.InitiatorUserId,
            job.Status,
            job.Progress,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.ResultMessage,
            job.Error
        });
    }
}