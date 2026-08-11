using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services;

public interface IUpdateBackupService
{
    Task<UpdateBackupResult> CreateBackupAsync(CancellationToken ct = default);
}

public sealed record UpdateBackupResult(string FilePath, long SizeBytes);

public sealed class UpdateBackupService : IUpdateBackupService
{
    private const string BackupPrefix = "update-backup-";
    private const string BackupExtension = ".zip";
    private readonly IExportService _exportService;
    private readonly IOptions<UpdateBackupOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<UpdateBackupService> _logger;

    public UpdateBackupService(
        IExportService exportService,
        IOptions<UpdateBackupOptions> options,
        IHostEnvironment environment,
        ILogger<UpdateBackupService> logger)
    {
        _exportService = exportService;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task<UpdateBackupResult> CreateBackupAsync(CancellationToken ct = default)
    {
        var options = ValidateOptions(_options.Value);
        var backupDirectory = ResolveBackupDirectory(options.Directory);
        Directory.CreateDirectory(backupDirectory);

        var fileName = CreateBackupFileName();
        var finalPath = GetSafePath(backupDirectory, fileName);
        var tempPath = GetSafePath(backupDirectory, $"{fileName}.{Guid.NewGuid():N}.tmp");

        _logger.LogInformation(
            "Starting update backup to {BackupDirectory} (includeImages={IncludeImages}, includePdf={IncludePdf})",
            backupDirectory,
            options.IncludeImages,
            options.IncludePdf);

        try
        {
            await using (var exportStream = await _exportService
                .ExportAllAsync(options.SystemInitiatorUserId, options.IncludeImages, options.IncludePdf, ct)
                .ConfigureAwait(false))
            await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await exportStream.CopyToAsync(target, ct).ConfigureAwait(false);
            }

            File.Move(tempPath, finalPath);
            var size = new FileInfo(finalPath).Length;

            await ApplyRetentionAsync(backupDirectory, finalPath, options.RetentionCount, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Update backup created at {BackupPath} with {SizeBytes} bytes",
                finalPath,
                size);

            return new UpdateBackupResult(finalPath, size);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TryDeleteTempFile(tempPath);
            _logger.LogError(ex, "Update backup failed. Installation must not continue.");
            throw;
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    private static UpdateBackupOptions ValidateOptions(UpdateBackupOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Directory))
        {
            throw new InvalidOperationException("Update backup directory is not configured.");
        }

        if (options.RetentionCount < 1)
        {
            throw new InvalidOperationException("Update backup retention count must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(options.SystemInitiatorUserId))
        {
            throw new InvalidOperationException("Update backup system initiator user id is not configured.");
        }

        return options;
    }

    private string ResolveBackupDirectory(string configuredDirectory)
    {
        var combined = Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.Combine(_environment.ContentRootPath, configuredDirectory);

        return Path.GetFullPath(combined);
    }

    private static string CreateBackupFileName()
        => $"{BackupPrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}Z{BackupExtension}";

    private static string GetSafePath(string backupDirectory, string fileName)
    {
        if (fileName != Path.GetFileName(fileName))
        {
            throw new InvalidOperationException("Backup file name must not contain path segments.");
        }

        var root = Path.GetFullPath(backupDirectory);
        var path = Path.GetFullPath(Path.Combine(root, fileName));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Backup path escaped the configured backup directory.");
        }

        return path;
    }

    private async Task ApplyRetentionAsync(string backupDirectory, string currentBackupPath, int retentionCount, CancellationToken ct)
    {
        var backups = Directory.EnumerateFiles(backupDirectory, $"{BackupPrefix}*{BackupExtension}", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(GetSafePath(backupDirectory, Path.GetFileName(path))))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.Ordinal)
            .Skip(retentionCount)
            .ToList();

        foreach (var backup in backups)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                backup.Delete();
                _logger.LogInformation("Deleted old update backup {BackupPath}", backup.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old update backup {BackupPath}. Installation must not continue.", backup.FullName);
                throw new IOException($"Failed to apply update backup retention for '{backup.FullName}'.", ex);
            }

            await Task.Yield();
        }

        if (!File.Exists(currentBackupPath))
        {
            throw new IOException("Current update backup was not retained.");
        }
    }

    private void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temporary update backup file {TempPath}", tempPath);
        }
    }
}
