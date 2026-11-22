using System.Collections;
using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Deterministic file-I/O tests for the best-effort temporary artifact cleanup.
/// </summary>
public sealed class TempDirectoryCleanupTests
{
    /// <summary>
    /// A directory containing a read-only file is removed completely when attribute
    /// normalization is enabled.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_ExistingDirectoryWithReadOnlyFile_DeletesCompletely()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var readOnlyFile = Path.Combine(root, "readonly.txt");
        File.WriteAllText(readOnlyFile, "x");
        File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);

        TempDirectoryCleanup.DeleteBestEffort(root, normalizeAttributes: true);

        Directory.Exists(root).Should().BeFalse();
    }

    /// <summary>
    /// An exclusively opened file makes every delete attempt fail — the method still
    /// returns without an exception, because cleanup must never mask the actual assert.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_LockedDirectory_DoesNotThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var lockedFile = Path.Combine(root, "locked.txt");
        File.WriteAllText(lockedFile, "x");
        var stream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            var act = () => TempDirectoryCleanup.DeleteBestEffort(root);

            act.Should().NotThrow("a cleanup failure must never mask the actual assert");
        }
        finally
        {
            stream.Dispose();
            TempDirectoryCleanup.DeleteBestEffort(root);
        }
    }

    /// <summary>
    /// A path that does not exist is tolerated.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_MissingDirectory_DoesNotThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-missing-" + Guid.NewGuid().ToString("N"));

        var act = () => TempDirectoryCleanup.DeleteBestEffort(root);

        act.Should().NotThrow();
    }

    /// <summary>
    /// The kill safety net terminates registered processes before deleting; an already
    /// disposed process object is tolerated. Spawns real placeholder processes, hence
    /// the <c>OsInterface</c> classification.
    /// </summary>
    [Fact]
    [Trait(TestCategories.Category, TestCategories.OsInterface)]
    public void DeleteBestEffort_ProcessesToKill_KillsProcessAndDeletesDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "x.txt"), "x");

        var process = Process.Start(LongRunningPlaceholderStartInfo())!;
        var disposedProcess = Process.Start(QuickExitPlaceholderStartInfo())!;
        disposedProcess.WaitForExit(10_000);
        disposedProcess.Dispose();
        try
        {
            TempDirectoryCleanup.DeleteBestEffort(root, processesToKill: [process, disposedProcess]);

            process.HasExited.Should().BeTrue("registered processes are killed before the delete");
            Directory.Exists(root).Should().BeFalse();
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.Dispose();
            TempDirectoryCleanup.DeleteBestEffort(root);
        }
    }

    /// <summary>
    /// A caller-side sequence whose enumerator throws is covered by the never-throws
    /// contract as well — the enumeration is part of the protected kill phase, and the
    /// delete phase still runs afterwards.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_ProcessesToKillEnumerationThrows_StillDeletesDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "x.txt"), "x");

        var act = () => TempDirectoryCleanup.DeleteBestEffort(
            root, processesToKill: new ThrowingProcessEnumerable());

        act.Should().NotThrow("the kill phase is part of the never-throws contract");
        Directory.Exists(root).Should().BeFalse("the delete phase still runs after a failed kill enumeration");
    }

    /// <summary>
    /// An existing file is deleted.
    /// </summary>
    [Fact]
    public void DeleteFileBestEffort_ExistingFile_DeletesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "tempfile-cleanup-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "x");

        TempDirectoryCleanup.DeleteFileBestEffort(path);

        File.Exists(path).Should().BeFalse();
    }

    /// <summary>
    /// A read-only file is deleted after its attributes are normalized.
    /// </summary>
    [Fact]
    public void DeleteFileBestEffort_ReadOnlyFile_DeletesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "tempfile-cleanup-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "x");
        File.SetAttributes(path, FileAttributes.ReadOnly);

        TempDirectoryCleanup.DeleteFileBestEffort(path);

        File.Exists(path).Should().BeFalse();
    }

    /// <summary>
    /// A missing file is a no-op instead of an error — callers need no
    /// <see cref="File.Exists(string)"/> guard.
    /// </summary>
    [Fact]
    public void DeleteFileBestEffort_MissingFile_DoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), "tempfile-cleanup-missing-" + Guid.NewGuid().ToString("N"));

        var act = () => TempDirectoryCleanup.DeleteFileBestEffort(path);

        act.Should().NotThrow();
    }

    /// <summary>
    /// An exclusively opened file makes every delete attempt fail — the method still
    /// returns without an exception, so a <c>finally</c>-block delete cannot mask the
    /// test outcome.
    /// </summary>
    [Fact]
    public void DeleteFileBestEffort_LockedFile_DoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), "tempfile-cleanup-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "x");
        var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            var act = () => TempDirectoryCleanup.DeleteFileBestEffort(path);

            act.Should().NotThrow("a cleanup failure must never mask the actual assert");
        }
        finally
        {
            stream.Dispose();
            TempDirectoryCleanup.DeleteFileBestEffort(path);
        }
    }

    private sealed class ThrowingProcessEnumerable : IEnumerable<Process>
    {
        public IEnumerator<Process> GetEnumerator()
        {
            // Deliberately fail during enumeration to exercise the never-throw contract.
            var message = "Simulated enumerator failure.";
            throw new InvalidOperationException(message);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static ProcessStartInfo LongRunningPlaceholderStartInfo()
    {
        var startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("ping", "-n 300 127.0.0.1")
            : new ProcessStartInfo("/bin/sh", "-c \"sleep 300\"");
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        return startInfo;
    }

    private static ProcessStartInfo QuickExitPlaceholderStartInfo()
    {
        var startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", "/c exit 0")
            : new ProcessStartInfo("/bin/sh", "-c \"exit 0\"");
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        return startInfo;
    }
}
