using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for <see cref="PluginWorkspaceProvider"/>: candidate order, writability probing and the
/// failure translation into <see cref="PluginWorkspaceException"/>.
/// </summary>
public sealed class PluginWorkspaceProviderTests : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// A configured work directory is used as the first candidate.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldUseConfiguredWorkDirectory()
    {
        using var workspace = new TempWorkspace();
        var configured = Path.Combine(workspace.Root, "configured-work");
        var sut = CreateSut(workspace, new PluginUpdateOptions { WorkDirectory = configured });

        var result = sut.CreateWorkDirectory("rezepte-plugin-download");

        result.Should().StartWith(Path.GetFullPath(configured));
        Directory.Exists(result).Should().BeTrue();
    }

    /// <summary>
    /// Without a configured work directory the system temp directory is used.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldUseSystemTemp_WhenNoConfig()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateSut(workspace);

        var result = sut.CreateWorkDirectory("rezepte-plugin-download");

        try
        {
            result.Should().StartWith(Path.GetFullPath(Path.GetTempPath()));
            Directory.Exists(result).Should().BeTrue();
        }
        finally
        {
            TempDirectoryCleanup.DeleteBestEffort(result);
        }
    }

    /// <summary>
    /// When the system temp candidate fails the provider falls back to the managed plugins
    /// work directory below the content root.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldFallBackToPluginsWork_WhenTempFails()
    {
        using var workspace = new TempWorkspace();
        var blocked = CreateBlockedPath(workspace.Root);
        var sut = CreateSut(workspace, systemTempPath: () => blocked);

        var result = sut.CreateWorkDirectory("rezepte-plugin-download");

        result.Should().StartWith(Path.Combine(workspace.Root, "plugins", ".work"));
        Directory.Exists(result).Should().BeTrue();
    }

    /// <summary>
    /// When no candidate can provide a work directory a <see cref="PluginWorkspaceException"/>
    /// carrying the well-known error constant is thrown.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldThrowWorkspaceException_WhenAllCandidatesFail()
    {
        using var workspace = new TempWorkspace();
        var blocked = CreateBlockedPath(workspace.Root);
        // A file named "plugins" makes the content-root candidate non-creatable as well.
        File.WriteAllText(Path.Combine(workspace.Root, "plugins"), "blocked");
        var sut = CreateSut(workspace, new PluginUpdateOptions { WorkDirectory = blocked }, () => blocked);

        var act = () => sut.CreateWorkDirectory("rezepte-plugin-download");

        var error = act.Should().Throw<PluginWorkspaceException>();
        error.Which.Message.Should().Be(PluginUpdateCheckErrors.WorkDirectoryUnavailable);
    }

    /// <summary>
    /// The returned directory has the structure <c>{base}/{purpose}/{guid}</c> and exists.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldCreatePurposeAndGuidLevels()
    {
        using var workspace = new TempWorkspace();
        var configured = Path.Combine(workspace.Root, "base");
        var sut = CreateSut(workspace, new PluginUpdateOptions { WorkDirectory = configured });

        var result = sut.CreateWorkDirectory("rezepte-plugin-backup");

        Path.GetFileName(Path.GetDirectoryName(result)).Should().Be("rezepte-plugin-backup");
        Guid.TryParseExact(Path.GetFileName(result), "N", out _).Should().BeTrue();
        Path.GetFullPath(result).Should().StartWith(Path.GetFullPath(configured));
        Directory.Exists(result).Should().BeTrue();
    }

    /// <summary>
    /// Orphaned work directories below the managed <c>plugins/.work</c> fallback are removed
    /// best-effort when a new work directory is created, while the new directory is kept.
    /// Only provably stale entries (older than the staleness cutoff) are removed.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldCleanUpStalePluginsWorkDirectories()
    {
        using var workspace = new TempWorkspace();
        var staleWorkDirectory = Directory.CreateDirectory(Path.Combine(workspace.Root, "plugins", ".work", "rezepte-plugin-download", "stale-dir")).FullName;
        MarkAsStale(staleWorkDirectory);
        var otherPurposeDirectory = Directory.CreateDirectory(Path.Combine(workspace.Root, "plugins", ".work", "rezepte-plugin-backup", "other-purpose")).FullName;
        var blocked = CreateBlockedPath(workspace.Root);
        var sut = CreateSut(workspace, systemTempPath: () => blocked);

        var result = sut.CreateWorkDirectory("rezepte-plugin-download");

        Directory.Exists(staleWorkDirectory).Should().BeFalse("stale directories of the same purpose below the .work fallback are orphaned and must be removed");
        Directory.Exists(otherPurposeDirectory).Should().BeTrue("directories of other purposes may still belong to the in-flight run and must not be touched");
        Directory.Exists(result).Should().BeTrue("the newly created work directory must survive the cleanup");
        result.Should().StartWith(Path.Combine(workspace.Root, "plugins", ".work"));
    }

    /// <summary>
    /// A recently created same-purpose directory below <c>plugins/.work</c> may still be in use
    /// (by another provider instance or a second process sharing the content root) and must not
    /// be removed — only provably stale entries count as orphans.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldKeepRecentSamePurposeDirectories()
    {
        using var workspace = new TempWorkspace();
        var recentDirectory = Directory.CreateDirectory(Path.Combine(workspace.Root, "plugins", ".work", "rezepte-plugin-backup", "in-flight")).FullName;
        var blocked = CreateBlockedPath(workspace.Root);
        var sut = CreateSut(workspace, systemTempPath: () => blocked);

        var result = sut.CreateWorkDirectory("rezepte-plugin-backup");

        Directory.Exists(recentDirectory).Should().BeTrue("a young same-purpose directory may still be in use and must survive the cleanup");
        Directory.Exists(result).Should().BeTrue();
    }

    /// <summary>
    /// Work directories handed out by the same provider instance stay in use until their callers
    /// clean them up — e.g. the installer holds a first backup for rollback while requesting a
    /// second one — and must never be removed, not even when aged artificially.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldKeepDirectoriesHandedOutByThisInstance()
    {
        using var workspace = new TempWorkspace();
        var blocked = CreateBlockedPath(workspace.Root);
        var sut = CreateSut(workspace, systemTempPath: () => blocked);
        var first = sut.CreateWorkDirectory("rezepte-plugin-backup");
        MarkAsStale(first);

        var second = sut.CreateWorkDirectory("rezepte-plugin-backup");

        Directory.Exists(first).Should().BeTrue("the first same-purpose work directory may still be registered for rollback and must survive");
        Directory.Exists(second).Should().BeTrue();
    }

    /// <summary>
    /// Candidates differing only in casing are deduplicated on Windows but probed as distinct
    /// candidates on case-sensitive file systems.
    /// </summary>
    [Fact]
    public void CreateWorkDirectory_ShouldDeduplicateCandidatesMatchingTheFileSystemCasing()
    {
        using var workspace = new TempWorkspace();
        var blocked = CreateBlockedPath(workspace.Root);
        var caseVariant = Path.Combine(workspace.Root, "BLOCKER", "unreachable");
        var logger = new RecordingLogger<PluginWorkspaceProvider>();
        var sut = new PluginWorkspaceProvider(
            Options.Create(new PluginUpdateOptions { WorkDirectory = blocked }),
            new TestHostEnvironment(workspace.Root),
            logger,
            () => caseVariant);

        var result = sut.CreateWorkDirectory("rezepte-plugin-download");

        if (OperatingSystem.IsWindows())
        {
            result.Should().StartWith(Path.Combine(workspace.Root, "plugins", ".work"));
            logger.Messages.Should().ContainSingle(m => m.Contains(blocked), "the case-variant duplicate must be skipped");
        }
        else
        {
            var probed = result.StartsWith(caseVariant, StringComparison.Ordinal)
                || logger.Messages.Any(m => m.Contains(caseVariant));
            probed.Should().BeTrue("on case-sensitive file systems the case-variant is a distinct candidate that must be probed");
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private static PluginWorkspaceProvider CreateSut(
        TempWorkspace workspace,
        PluginUpdateOptions? updateOptions = null,
        Func<string>? systemTempPath = null)
        => new(
            Options.Create(updateOptions ?? new PluginUpdateOptions()),
            new TestHostEnvironment(workspace.Root),
            NullLogger<PluginWorkspaceProvider>.Instance,
            systemTempPath);

    private static string CreateBlockedPath(string root)
    {
        var blockerFile = Path.Combine(root, "blocker");
        File.WriteAllText(blockerFile, "blocked");
        return Path.Combine(blockerFile, "unreachable");
    }

    // Ages a directory beyond the provider's staleness cutoff. Both timestamps are set because
    // the stale check requires the newest of them to predate the cutoff; creation time cannot
    // be set on all platforms (it then maps onto the write time), so both are set best-effort.
    private static void MarkAsStale(string directory)
    {
        var staleTime = DateTime.UtcNow - TimeSpan.FromDays(2);
        try
        {
            Directory.SetCreationTimeUtc(directory, staleTime);
        }
        catch (PlatformNotSupportedException)
        {
            // The last write time below still marks the directory as stale.
        }

        Directory.SetLastWriteTimeUtc(directory, staleTime);
    }
}
