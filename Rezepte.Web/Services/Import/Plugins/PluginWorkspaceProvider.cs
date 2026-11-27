using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Default <see cref="IPluginWorkspaceProvider"/> implementation that probes an ordered list of
/// base directory candidates — the configured <c>PluginUpdates:WorkDirectory</c>, the system temp
/// directory and the guaranteed writable <c>{ContentRootPath}/plugins/.work</c> fallback — by
/// creating the requested work directory (including all intermediate levels) underneath them.
/// Provably stale work directories of the same purpose left below the managed <c>.work</c>
/// fallback by failed best-effort cleanups or aborted runs are removed on each call.
/// </summary>
/// <param name="options">The options parameter.</param>
/// <param name="environment">The environment parameter.</param>
/// <param name="logger">The logger parameter.</param>
/// <param name="systemTempPath">Optional system temp path resolution seam; defaults to <see cref="Path.GetTempPath"/>.</param>
/// <returns>The result.</returns>
public sealed class PluginWorkspaceProvider(
    IOptions<PluginUpdateOptions> options,
    IHostEnvironment environment,
    ILogger<PluginWorkspaceProvider> logger,
    Func<string>? systemTempPath = null) : IPluginWorkspaceProvider
{
    private const string PluginsWorkDirectoryName = ".work";

    // Only directories provably older than this count as orphaned: a plugin update run holds
    // its work directories for minutes at most, so a directory this old can only be a leftover
    // of an aborted run. Anything younger may still be in use by this or another process.
    private static readonly TimeSpan StaleWorkDirectoryAge = TimeSpan.FromDays(1);

    // File system paths are case-insensitive on Windows but case-sensitive elsewhere.
    private static readonly StringComparer CandidatePathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    // Work directories handed out by this instance stay in use until their callers clean them
    // up (the installer keeps same-purpose backups registered for a possible rollback), so the
    // janitor must never remove them — regardless of their timestamps.
    private readonly HashSet<string> activeWorkDirectories = new(CandidatePathComparer);

    /// <inheritdoc />
    public string CreateWorkDirectory(string purpose)
    {
        Exception? lastError = null;
        var visited = new HashSet<string>(CandidatePathComparer);
        foreach (var candidate in GetBaseDirectoryCandidates())
        {
            if (string.IsNullOrWhiteSpace(candidate) || !visited.Add(candidate))
            {
                continue;
            }

            var workDirectory = Path.Combine(candidate, purpose, Guid.NewGuid().ToString("N"));
            try
            {
                // CreateDirectory creates all intermediate levels and doubles as the
                // writability probe of the base directory candidate.
                var created = Directory.CreateDirectory(workDirectory).FullName;
                activeWorkDirectories.Add(created);
                CleanupStaleWorkDirectories(purpose, created);
                return created;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                lastError = ex;
                logger.LogWarning(ex, "Could not create plugin work directory for purpose {WorkDirectoryPurpose} under {WorkDirectoryBase}", purpose, candidate);
            }
        }

        throw new PluginWorkspaceException(lastError);
    }

    private IEnumerable<string?> GetBaseDirectoryCandidates()
    {
        yield return options.Value.WorkDirectory;
        yield return ResolveSystemTempPath();
        yield return Path.Combine(environment.ContentRootPath, "plugins", PluginsWorkDirectoryName);
    }

    private string? ResolveSystemTempPath()
    {
        try
        {
            return (systemTempPath ?? Path.GetTempPath)();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            logger.LogWarning(ex, "Could not resolve the system temp directory for plugin work directories");
            return null;
        }
    }

    // Removes orphaned work directories of the same purpose below the managed
    // {ContentRoot}/plugins/.work fallback: unlike the system temp directory, nothing else
    // cleans them up after failed best-effort deletes or aborted runs. Only the same purpose
    // level is touched — other purposes may still hold directories of the in-flight run.
    // A directory is removed only when it is provably stale (not handed out by this instance
    // and older than StaleWorkDirectoryAge); a still-active directory of this or another
    // process sharing the content root is indistinguishable from an orphan and must survive.
    // Never throws — a cleanup failure must not break provisioning.
    private void CleanupStaleWorkDirectories(string purpose, string activeWorkDirectory)
    {
        var purposeDirectory = Path.Combine(environment.ContentRootPath, "plugins", PluginsWorkDirectoryName, purpose);
        try
        {
            if (!Directory.Exists(purposeDirectory))
            {
                return;
            }

            var staleBefore = DateTime.UtcNow - StaleWorkDirectoryAge;
            foreach (var directory in Directory.EnumerateDirectories(purposeDirectory))
            {
                if (string.Equals(directory, activeWorkDirectory, StringComparison.Ordinal)
                    || activeWorkDirectories.Contains(directory)
                    || !IsStale(directory, staleBefore))
                {
                    continue;
                }

                TryDeleteDirectory(directory);
            }

            if (!Directory.EnumerateFileSystemEntries(purposeDirectory).Any())
            {
                TryDeleteEmptyDirectory(purposeDirectory);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clean up stale plugin work directories under {PluginWorkDirectoryBase}", purposeDirectory);
        }
    }

    // A directory counts as stale only when even its newest timestamp — the later of creation
    // and last write — predates the cutoff. Creation time can be unavailable on some file
    // systems, so both are checked; any doubt or read failure keeps the directory.
    private static bool IsStale(string directory, DateTime staleBefore)
    {
        try
        {
            var info = new DirectoryInfo(directory);
            return info.CreationTimeUtc < staleBefore && info.LastWriteTimeUtc < staleBefore;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not delete stale plugin work directory {PluginWorkDirectory}", directory);
        }
    }

    // Deletes the purpose level only when it is still empty at delete time: the non-recursive
    // delete fails on a work directory appearing between the emptiness check and the delete,
    // so no freshly created directory can be removed underneath it.
    private void TryDeleteEmptyDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Expected when a new work directory appeared after the emptiness check.
            logger.LogDebug(ex, "Could not delete empty plugin work purpose directory {PluginWorkDirectory}", directory);
        }
    }
}
