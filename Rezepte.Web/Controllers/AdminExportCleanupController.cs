using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/admin/exports/cleanup")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminExportCleanupController : ApiControllerBase
{
    private readonly IExportCleanupService _cleanupService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AdminExportCleanupController> _logger;

    public AdminExportCleanupController(
        IExportCleanupService cleanupService,
        TimeProvider timeProvider,
        ILogger<AdminExportCleanupController> logger)
    {
        _cleanupService = cleanupService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public sealed record ExportCleanupSettingsDto(string CleanupTime, DateTimeOffset? LastRunAt);

    public sealed record UpdateExportCleanupSettingsRequest(string? CleanupTime);

    public sealed record ExportCleanupRunDto(int DeletedFiles, int DeletedRecords, DateTimeOffset RunAt);

    /// <summary>
    /// GET /api/admin/exports/cleanup — returns the configured cleanup time (HH:mm, server local time) and the last run.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ExportCleanupSettingsDto>> GetSettings(CancellationToken ct = default)
    {
        var settings = await _cleanupService.GetSettingsAsync(ct).ConfigureAwait(false);
        return Ok(ToDto(settings));
    }

    /// <summary>
    /// PUT /api/admin/exports/cleanup — sets the daily cleanup time (HH:mm, server local time).
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ExportCleanupSettingsDto>> UpdateSettings([FromBody] UpdateExportCleanupSettingsRequest request, CancellationToken ct = default)
    {
        if (!TimeOnly.TryParseExact(request.CleanupTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cleanupTime))
        {
            return BadRequest(new ProblemDetails { Title = "Invalid cleanup time", Detail = "Cleanup time must be in the format HH:mm." });
        }

        var settings = await _cleanupService.SetCleanupTimeAsync(cleanupTime, ct).ConfigureAwait(false);
        _logger.LogInformation("Admin {AdminId} set export cleanup time to {CleanupTime}", GetUserId(), cleanupTime);
        return Ok(ToDto(settings));
    }

    /// <summary>
    /// POST /api/admin/exports/cleanup/run — runs the cleanup immediately.
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<ExportCleanupRunDto>> Run(CancellationToken ct = default)
    {
        var result = await _cleanupService.RunCleanupAsync(_timeProvider.GetLocalNow(), ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Admin {AdminId} triggered export cleanup manually: {DeletedFiles} files, {DeletedRecords} records",
            GetUserId(),
            result.DeletedFiles,
            result.DeletedRecords);
        return Ok(new ExportCleanupRunDto(result.DeletedFiles, result.DeletedRecords, result.RunAt));
    }

    private static ExportCleanupSettingsDto ToDto(ExportCleanupSettings settings)
        => new(settings.CleanupTime.ToString("HH:mm", CultureInfo.InvariantCulture), settings.LastRunAt);
}
