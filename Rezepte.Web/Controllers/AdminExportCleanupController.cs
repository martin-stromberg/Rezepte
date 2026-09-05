using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Represents the admin export cleanup controller class.
/// </summary>
[ApiController]
[Route("api/admin/exports/cleanup")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminExportCleanupController : ApiControllerBase
{
    private readonly IExportCleanupService _cleanupService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AdminExportCleanupController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminExportCleanupController"/> class.
    /// </summary>
    /// <param name="cleanupService">The cleanup service parameter.</param>
    /// <param name="timeProvider">The time provider parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    public AdminExportCleanupController(
        IExportCleanupService cleanupService,
        TimeProvider timeProvider,
        ILogger<AdminExportCleanupController> logger)
    {
        _cleanupService = cleanupService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// exports the cleanup settings dto.
    /// </summary>
    /// <param name="CleanupTime">The cleanup time parameter.</param>
    /// <param name="LastRunAt">The last run at parameter.</param>
    /// <returns>The result.</returns>
    public sealed record ExportCleanupSettingsDto(string CleanupTime, DateTimeOffset? LastRunAt);

    /// <summary>
    /// Updates the export cleanup settings request.
    /// </summary>
    /// <param name="CleanupTime">The cleanup time parameter.</param>
    /// <returns>The result.</returns>
    public sealed record UpdateExportCleanupSettingsRequest(string? CleanupTime);

    /// <summary>
    /// exports the cleanup run dto.
    /// </summary>
    /// <param name="DeletedFiles">The deleted files parameter.</param>
    /// <param name="DeletedRecords">The deleted records parameter.</param>
    /// <param name="RunAt">The run at parameter.</param>
    /// <returns>The result.</returns>
    public sealed record ExportCleanupRunDto(int DeletedFiles, int DeletedRecords, DateTimeOffset RunAt);

    /// <summary>
    /// GET /api/admin/exports/cleanup — returns the configured cleanup time (HH:mm, server local time) and the last run.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    [HttpGet]
    public async Task<ActionResult<ExportCleanupSettingsDto>> GetSettings(CancellationToken ct = default)
    {
        var settings = await _cleanupService.GetSettingsAsync(ct).ConfigureAwait(false);
        return Ok(ToDto(settings));
    }

    /// <summary>
    /// PUT /api/admin/exports/cleanup — sets the daily cleanup time (HH:mm, server local time).
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
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
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
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
