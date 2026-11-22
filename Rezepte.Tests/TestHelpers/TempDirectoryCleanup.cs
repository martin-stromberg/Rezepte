using System.Diagnostics;

namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Best-effort deletion of temporary test artifacts: optionally registered processes are
/// killed first, then the directory or file is deleted with retry/backoff. Residual errors
/// are only reported diagnostically instead of being thrown, so a cleanup failure never
/// masks the actual test assertion (no <see cref="AggregateException"/> overlay from a
/// <c>Dispose</c>/<c>finally</c> path).
/// </summary>
internal static class TempDirectoryCleanup
{
    private const int MaxDeleteAttempts = 5;
    private static readonly TimeSpan ProcessKillWaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Kills the optionally registered processes and then deletes <paramref name="root"/>
    /// best-effort. Never throws — a final residual error only goes to
    /// <see cref="Console.Error"/>.
    /// </summary>
    /// <param name="root">The directory to delete.</param>
    /// <param name="processesToKill">Optional child processes to kill before deleting
    /// (kill safety net; already disposed process objects are tolerated).</param>
    /// <param name="normalizeAttributes">Reset file attributes to <see cref="FileAttributes.Normal"/>
    /// before deleting (e.g. read-only objects in git test repositories).</param>
    internal static void DeleteBestEffort(
        string root,
        IEnumerable<Process>? processesToKill = null,
        bool normalizeAttributes = false)
    {
        try
        {
            // The enumeration of the caller-side IEnumerable happens inside this call
            // (GetEnumerator/MoveNext of the foreach) and is part of the never-throws
            // contract as well — it must not sit unprotected before the try.
            KillProcesses(processesToKill);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(
                $"TempDirectoryCleanup: could not kill the registered processes for '{root}': {error.Message}");
        }

        try
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            if (normalizeAttributes)
            {
                TryNormalizeDirectoryAttributes(root);
            }

            DeleteWithRetry(() => Directory.Delete(root, recursive: true), root);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"TempDirectoryCleanup: could not delete '{root}': {error.Message}");
        }
    }

    /// <summary>
    /// Deletes a single temporary file best-effort. A missing path is a no-op, so callers
    /// do not need a <see cref="File.Exists(string)"/> guard. Never throws — a residual
    /// error only goes to <see cref="Console.Error"/>, so a <c>finally</c>-block delete can
    /// no longer mask the test outcome.
    /// </summary>
    /// <param name="path">The file to delete.</param>
    internal static void DeleteFileBestEffort(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            catch (Exception)
            {
                // Best effort: attribute normalization is preparation, not a requirement.
            }

            DeleteWithRetry(() => File.Delete(path), path);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"TempDirectoryCleanup: could not delete '{path}': {error.Message}");
        }
    }

    private static void KillProcesses(IEnumerable<Process>? processesToKill)
    {
        if (processesToKill is null)
        {
            return;
        }

        foreach (var process in processesToKill)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    if (!process.WaitForExit(ProcessKillWaitTimeout))
                    {
                        Console.Error.WriteLine(
                            $"TempDirectoryCleanup: process {process.Id} was still running "
                            + $"{ProcessKillWaitTimeout.TotalSeconds}s after Kill — deleting anyway.");
                    }
                }
            }
            catch (Exception)
            {
                // Best effort: an already disposed process object (ObjectDisposedException),
                // a process that no longer exists or a Win32 error must not abort the cleanup.
            }
        }
    }

    private static void TryNormalizeDirectoryAttributes(string root)
    {
        try
        {
            // EnumerateFiles evaluates lazily: an inaccessible subdirectory only throws
            // mid-enumeration. That failure must not skip the delete retry loop in the
            // caller — Directory.Delete may still succeed.
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch (Exception)
                {
                    // Best effort: attribute normalization is preparation, not a requirement.
                }
            }
        }
        catch (Exception)
        {
            // Best effort: attribute normalization is preparation, not a requirement.
        }
    }

    private static void DeleteWithRetry(Action delete, string target)
    {
        for (var attempt = 1; attempt <= MaxDeleteAttempts; attempt++)
        {
            try
            {
                delete();
                return;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                if (attempt == MaxDeleteAttempts)
                {
                    Console.Error.WriteLine(
                        $"TempDirectoryCleanup: '{target}' could not be deleted after "
                        + $"{MaxDeleteAttempts} attempts: {error.Message}");
                    return;
                }

                Thread.Sleep(100 * attempt);
            }
        }
    }
}
