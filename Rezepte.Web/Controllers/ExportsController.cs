using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ExportsController : ControllerBase
{
    private readonly IBackgroundJobQueue _jobQueue;
    private readonly ILogger<ExportsController> _logger;

    public ExportsController(IBackgroundJobQueue jobQueue, ILogger<ExportsController> logger)
    {
        _jobQueue = jobQueue;
        _logger = logger;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Exportiert die Rezeptesammlung des angemeldeten Benutzers.
    /// Query: ?format=csv|json (default zip), ?includePdf=true|false, ?includeImages=true|false
    /// Startet einen Hintergrundjob und liefert dessen Job-ID zurueck.
    /// </summary>
    [HttpGet("recipes")]
    public async Task<IActionResult> ExportMyRecipes([FromQuery] string format = "json", [FromQuery] bool includeImages = false, [FromQuery] bool includePdf = false, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        _logger.LogInformation("User {UserId} requested export job format={Format} includePdf={IncludePdf} includeImages={includeImages}", userId, format, includePdf, includeImages);

        try
        {
            var payloadJson = new ExportJobPayload(includeImages, includePdf).ToJson();
            var jobId = await _jobQueue.EnqueueAsync("export:user", payloadJson, userId, ct).ConfigureAwait(false);
            return Accepted(new
            {
                jobId,
                statusUrl = Url.ActionLink("GetJobStatus", "Jobs", new { id = jobId }),
                downloadUrl = Url.ActionLink("DownloadJobResult", "Jobs", new { id = jobId })
            });
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
    }
}
