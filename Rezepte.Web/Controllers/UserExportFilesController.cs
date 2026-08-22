using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)]
public class UserExportFilesController : ApiControllerBase
{
    private readonly RezepteDbContext _db;
    private readonly ExportJobFileStore _fileStore;
    private readonly ILogger<UserExportFilesController> _logger;

    public UserExportFilesController(RezepteDbContext db, ExportJobFileStore fileStore, ILogger<UserExportFilesController> logger)
    {
        _db = db;
        _fileStore = fileStore;
        _logger = logger;
    }

    /// <summary>
    /// List export files for the current user (admins can see all).
    /// GET /api/exports
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyFiles(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var query = isAdmin
            ? _db.UserExportFiles.AsNoTracking()
            : _db.UserExportFiles.AsNoTracking().Where(f => f.UserId == userId);

        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.FileName,
                f.Size,
                f.CreatedAt,
                f.IsAdminExport
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _logger.LogInformation("Listed {FileCount} export files", files.Count);

        return Ok(files);
    }

    /// <summary>
    /// Download a previously created export file.
    /// GET /api/exports/{id}/download
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var file = await _db.UserExportFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct)
            .ConfigureAwait(false);

        if (file is null || (!IsOwner(file, userId) && !User.IsInRole("Admin")))
            return NotFound();

        var filePath = _fileStore.GetPathForFileName(file.FileName);
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Export file {FileId} not found on disk at expected path", file.Id);
            return NotFound();
        }

        var downloadName = file.IsAdminExport
            ? $"export-all-{file.CreatedAt:yyyyMMddHHmm}.zip"
            : $"recipes-{file.CreatedAt:yyyyMMddHHmm}.zip";

        _logger.LogInformation(
            "Serving export file {FileId} as {DownloadName} ({Size} bytes)",
            file.Id,
            downloadName,
            file.Size);

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/zip", downloadName);
    }

    /// <summary>
    /// Delete an export file (physical file and database row).
    /// DELETE /api/exports/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var file = await _db.UserExportFiles
            .FirstOrDefaultAsync(f => f.Id == id, ct)
            .ConfigureAwait(false);

        if (file is null || (!IsOwner(file, userId) && !User.IsInRole("Admin")))
            return NotFound();

        try
        {
            var filePath = _fileStore.GetPathForFileName(file.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete export file {FileId} from disk", file.Id);
            return StatusCode(500, new ProblemDetails { Title = "Delete failed", Detail = "Could not remove the physical file." });
        }

        _db.UserExportFiles.Remove(file);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Deleted export file {FileId}", file.Id);

        return NoContent();
    }

    private bool IsOwner(UserExportFile file, string userId)
    {
        return string.Equals(file.UserId, userId, StringComparison.Ordinal);
    }
}
