using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Services;

public sealed record ExportCleanupSettings(TimeOnly CleanupTime, DateTimeOffset? LastRunAt);

public sealed record ExportCleanupResult(int DeletedFiles, int DeletedRecords, DateTimeOffset RunAt);

public interface IExportCleanupService
{
    Task<ExportCleanupSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<ExportCleanupSettings> SetCleanupTimeAsync(TimeOnly cleanupTime, CancellationToken ct = default);
    Task<bool> IsCleanupDueAsync(DateTimeOffset now, CancellationToken ct = default);
    Task<ExportCleanupResult> RunCleanupAsync(DateTimeOffset now, CancellationToken ct = default);
}

/// <summary>
/// Removes export archives (user exports and admin backups) from the exports directory
/// once they are older than <see cref="MaxAge"/>. The cleanup is scheduled once per day at an
/// admin-configurable local time; a missed run (application not running) is caught up on the
/// next opportunity.
/// </summary>
public sealed class ExportCleanupService : IExportCleanupService
{
    public const string CleanupTimeKey = "ExportCleanup:Time";
    public const string LastRunAtKey = "ExportCleanup:LastRunAt";
    public static readonly TimeOnly DefaultCleanupTime = new(3, 0);
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(1);

    private const string TimeFormat = "HH:mm";

    private readonly RezepteDbContext _db;
    private readonly ExportJobFileStore _fileStore;
    private readonly ILogger<ExportCleanupService> _logger;

    public ExportCleanupService(RezepteDbContext db, ExportJobFileStore fileStore, ILogger<ExportCleanupService> logger)
    {
        _db = db;
        _fileStore = fileStore;
        _logger = logger;
    }

    public async Task<ExportCleanupSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var rows = await _db.AppSettings
            .Where(s => s.Key == CleanupTimeKey || s.Key == LastRunAtKey)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var timeRaw = rows.FirstOrDefault(r => r.Key == CleanupTimeKey)?.Value;
        var cleanupTime = TimeOnly.TryParseExact(timeRaw, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime)
            ? parsedTime
            : DefaultCleanupTime;

        var lastRunRaw = rows.FirstOrDefault(r => r.Key == LastRunAtKey)?.Value;
        DateTimeOffset? lastRunAt = DateTimeOffset.TryParseExact(lastRunRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedLastRun)
            ? parsedLastRun
            : null;

        return new ExportCleanupSettings(cleanupTime, lastRunAt);
    }

    public async Task<ExportCleanupSettings> SetCleanupTimeAsync(TimeOnly cleanupTime, CancellationToken ct = default)
    {
        await UpsertAsync(CleanupTimeKey, cleanupTime.ToString(TimeFormat, CultureInfo.InvariantCulture), ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await GetSettingsAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsCleanupDueAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct).ConfigureAwait(false);
        return IsCleanupDue(settings, now);
    }

    /// <summary>
    /// The cleanup is due when the most recent scheduled occurrence (today at the configured time,
    /// or yesterday if that time has not been reached yet) lies after the last completed run.
    /// </summary>
    public static bool IsCleanupDue(ExportCleanupSettings settings, DateTimeOffset now)
    {
        var lastScheduled = GetLastScheduledOccurrence(settings.CleanupTime, now);
        return settings.LastRunAt is null || settings.LastRunAt.Value < lastScheduled;
    }

    public static DateTimeOffset GetLastScheduledOccurrence(TimeOnly cleanupTime, DateTimeOffset now)
    {
        var todayOccurrence = new DateTimeOffset(now.Date.Add(cleanupTime.ToTimeSpan()), now.Offset);
        return todayOccurrence <= now ? todayOccurrence : todayOccurrence.AddDays(-1);
    }

    public async Task<ExportCleanupResult> RunCleanupAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var threshold = now.UtcDateTime - MaxAge;
        var deletedFiles = 0;
        var deletedRecords = 0;

        var expired = await _db.UserExportFiles
            .Where(f => f.CreatedAt < threshold)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var file in expired)
        {
            ct.ThrowIfCancellationRequested();

            if (!TryDeleteRegisteredFile(file, out var fileDeleted))
            {
                continue;
            }

            if (fileDeleted)
            {
                deletedFiles++;
            }

            _db.UserExportFiles.Remove(file);
            deletedRecords++;
        }

        deletedFiles += await DeleteOrphanedArchivesAsync(threshold, ct).ConfigureAwait(false);

        await UpsertAsync(LastRunAtKey, now.ToString("O", CultureInfo.InvariantCulture), ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Export cleanup finished: {DeletedFiles} files and {DeletedRecords} records removed (threshold {Threshold:O})",
            deletedFiles,
            deletedRecords,
            threshold);

        return new ExportCleanupResult(deletedFiles, deletedRecords, now);
    }

    private bool TryDeleteRegisteredFile(UserExportFile file, out bool fileDeleted)
    {
        fileDeleted = false;
        string path;
        try
        {
            path = _fileStore.GetPathForFileName(file.FileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Export file {FileId} has an invalid file name and is skipped during cleanup", file.Id);
            return false;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                fileDeleted = true;
                _logger.LogInformation("Deleted expired export file {FileId} ({FileName})", file.Id, file.FileName);
            }
            else
            {
                _logger.LogInformation("Export file {FileId} ({FileName}) was already missing on disk; removing record", file.Id, file.FileName);
            }

            return true;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete expired export file {FileId} ({FileName})", file.Id, file.FileName);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Failed to delete expired export file {FileId} ({FileName})", file.Id, file.FileName);
            return false;
        }
    }

    private async Task<int> DeleteOrphanedArchivesAsync(DateTime threshold, CancellationToken ct)
    {
        var registered = (await _db.UserExportFiles.AsNoTracking()
                .Select(f => f.FileName)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(_fileStore.ExportsDirectory, "*.zip", SearchOption.TopDirectoryOnly))
        {
            ct.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(path);
            if (registered.Contains(fileName))
            {
                continue;
            }

            var info = new FileInfo(path);
            if (info.LastWriteTimeUtc >= threshold)
            {
                continue;
            }

            try
            {
                info.Delete();
                deleted++;
                _logger.LogInformation("Deleted orphaned export archive {FileName}", fileName);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to delete orphaned export archive {FileName}", fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Failed to delete orphaned export archive {FileName}", fileName);
            }
        }

        return deleted;
    }

    private async Task UpsertAsync(string key, string value, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync([key], ct).ConfigureAwait(false);
        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
    }
}
