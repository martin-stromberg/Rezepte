using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the plugin package installer tests.
/// </summary>
public sealed class PluginPackageInstallerTests : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Install async should install into versioned directory.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldInstallIntoVersionedDirectory()
    {
        using var workspace = new TempWorkspace();
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        await sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        File.ReadAllText(Path.Combine(workspace.PluginRoot, "PluginA@v1.0", "PluginA.dll")).Should().Be("replacement");
        var state = CreateRegistry(workspace).LoadState();
        state.Installs.Should().ContainSingle(e =>
            e.BaseName == "PluginA" &&
            e.DirectoryName == "PluginA@v1.0" &&
            e.ReleaseTag == "v1.0" &&
            e.SourceKey == "src");
    }

    /// <summary>
    /// Install async should mark the predecessor for deletion and keep it excluded until cleanup.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldMarkPredecessorPendingDeletion()
    {
        using var workspace = new TempWorkspace();
        var predecessor = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v0.9")).FullName;
        await File.WriteAllTextAsync(Path.Combine(predecessor, "PluginA.dll"), "original");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v0.9", ReleaseTag = "v0.9", SourceKey = "src" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        await sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        File.ReadAllText(Path.Combine(workspace.PluginRoot, "PluginA@v1.0", "PluginA.dll")).Should().Be("replacement");
        var persisted = registry.LoadState();
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginA" && e.DirectoryName == "PluginA@v1.0");
        persisted.PendingDeletes.Should().Contain("PluginA@v0.9");
        Directory.Exists(predecessor).Should().BeTrue("the stub plugin manager does not run the initialization cleanup");
    }

    /// <summary>
    /// Install async should mark dropped plugins of the same source as pending.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldMarkDroppedPluginOfSameSourcePending()
    {
        using var workspace = new TempWorkspace();
        var droppedDirectory = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginB@v1.0")).FullName;
        await File.WriteAllTextAsync(Path.Combine(droppedDirectory, "PluginB.dll"), "dropped");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "source-s" });
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginB", DirectoryName = "PluginB@v1.0", ReleaseTag = "v1.0", SourceKey = "source-s" });
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginC", DirectoryName = "PluginC@v1.0", ReleaseTag = "v1.0", SourceKey = "other-source" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        await sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v2.0", "source-s"));

        var persisted = registry.LoadState();
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginA" && e.DirectoryName == "PluginA@v2.0");
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginC" && e.SourceKey == "other-source");
        persisted.Installs.Should().NotContain(e => e.BaseName == "PluginB");
        persisted.PendingDeletes.Should().Contain("PluginB@v1.0");
        Directory.Exists(droppedDirectory).Should().BeTrue("the stub plugin manager does not run the initialization cleanup");
    }

    /// <summary>
    /// Install async should keep the previous version effective when the reload fails.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldKeepPreviousVersionEffective_WhenReloadFails()
    {
        using var workspace = new TempWorkspace();
        var predecessor = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v0.9")).FullName;
        await File.WriteAllTextAsync(Path.Combine(predecessor, "PluginA.dll"), "original");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v0.9", ReleaseTag = "v0.9", SourceKey = "src" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var pluginManager = new FailingPluginManager();
        var sut = CreateInstaller(workspace, pluginManager);

        var act = () => sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
        error.Which.Status.Should().Be(PluginSourceReleaseStatus.ReloadFailed);
        error.Which.Message.Should().Be("reload failed");
        pluginManager.InitializeCalls.Should().Be(2);
        File.ReadAllText(Path.Combine(predecessor, "PluginA.dll")).Should().Be("original");
        var persisted = registry.LoadState();
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginA" && e.DirectoryName == "PluginA@v0.9");
        Directory.Exists(Path.Combine(workspace.PluginRoot, "PluginA@v1.0")).Should().BeFalse();
        persisted.PendingDeletes.Should().Contain("PluginA@v1.0");
    }

    /// <summary>
    /// Install async should keep the registered directory when a reinstall with the same release tag fails on reload.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldKeepRegisteredDirectory_WhenSameTagReinstallFailsOnReload()
    {
        using var workspace = new TempWorkspace();
        var existing = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v1.0")).FullName;
        await File.WriteAllTextAsync(Path.Combine(existing, "PluginA.dll"), "original");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "src" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var pluginManager = new FailingPluginManager();
        var sut = CreateInstaller(workspace, pluginManager);

        var act = () => sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
        error.Which.Status.Should().Be(PluginSourceReleaseStatus.ReloadFailed);
        pluginManager.InitializeCalls.Should().Be(2);
        Directory.Exists(existing).Should().BeTrue("the restored state still registers this directory as the effective installation");
        File.ReadAllText(Path.Combine(existing, "PluginA.dll")).Should().Be("original", "the rollback restores the registered directory content from its backup");
        var persisted = registry.LoadState();
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginA" && e.DirectoryName == "PluginA@v1.0");
        persisted.PendingDeletes.Should().NotContain("PluginA@v1.0");
    }

    /// <summary>
    /// Install async should restore the registered directory when a reinstall with the same release tag fails during the copy.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldRestoreRegisteredDirectory_WhenSameTagReinstallFailsDuringCopy()
    {
        using var workspace = new TempWorkspace();
        var existing = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v1.0")).FullName;
        await File.WriteAllTextAsync(Path.Combine(existing, "PluginA.dll"), "original");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "src" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        var lockedFile = Path.Combine(incomingPlugin, "PluginA.dll");
        await File.WriteAllTextAsync(lockedFile, "replacement");
        await using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        var act = () => sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
        error.Which.Status.Should().Be(PluginSourceReleaseStatus.InstallFailed);
        File.ReadAllText(Path.Combine(existing, "PluginA.dll")).Should().Be("original", "the rollback restores the registered directory content from its backup");
        var persisted = registry.LoadState();
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginA" && e.DirectoryName == "PluginA@v1.0");
        persisted.PendingDeletes.Should().NotContain("PluginA@v1.0");
    }

    /// <summary>
    /// Install async should restore the state when the install fails before the replacement completes.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldRestoreState_WhenInstallFailsBeforeReplacement()
    {
        using var workspace = new TempWorkspace();
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginB")).FullName;
        var lockedFile = Path.Combine(incomingPlugin, "PluginB.dll");
        await File.WriteAllTextAsync(lockedFile, "partial");
        await using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var registry = CreateRegistry(workspace);
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        var act = () => sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
        error.Which.Status.Should().Be(PluginSourceReleaseStatus.InstallFailed);
        Directory.Exists(Path.Combine(workspace.PluginRoot, "PluginB@v1.0")).Should().BeFalse();
        registry.LoadState().Installs.Should().NotContain(e => e.BaseName == "PluginB");
    }

    /// <summary>
    /// Install async should keep the flat directory when a versioned install shadows it.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldKeepFlatDirectory_WhenVersionedInstallShadowsIt()
    {
        using var workspace = new TempWorkspace();
        var flatDirectory = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginX")).FullName;
        await File.WriteAllTextAsync(Path.Combine(flatDirectory, "PluginX.dll"), "flat-content");
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginX")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginX.dll"), "versioned-content");
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        await sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1", "src"));

        File.ReadAllText(Path.Combine(flatDirectory, "PluginX.dll")).Should().Be("flat-content");
        File.ReadAllText(Path.Combine(workspace.PluginRoot, "PluginX@v1", "PluginX.dll")).Should().Be("versioned-content");
        var persisted = CreateRegistry(workspace).LoadState();
        persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginX" && e.DirectoryName == "PluginX@v1");
        persisted.PendingDeletes.Should().NotContain("PluginX");
    }

    /// <summary>
    /// Install async should never persist a state that marks a registered directory for deletion.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldNotPersistPendingDeletion_WhenRegisteredDirectoryDeleteFails()
    {
        using var workspace = new TempWorkspace();
        var existing = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v1.0")).FullName;
        var lockedFile = Path.Combine(existing, "PluginA.dll");
        await File.WriteAllTextAsync(lockedFile, "original");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "src" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        await using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (!OperatingSystem.IsWindows())
        {
            // POSIX allows deleting open files; removing write permission on the
            // directory blocks the delete the same way the share mode does on Windows.
            File.SetUnixFileMode(existing, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }
        var recordingRegistry = new RecordingDirectoryRegistry(registry);
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager(), recordingRegistry);

        var act = () => sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        try
        {
            var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
            error.Which.Status.Should().Be(PluginSourceReleaseStatus.InstallFailed);
            recordingRegistry.SavedStates.Should().NotContain(saved =>
                saved.PendingDeletes.Any(pending => saved.Installs.Any(e => string.Equals(e.DirectoryName, pending, StringComparison.OrdinalIgnoreCase))),
                "a registered effective installation must never be persisted as pending deletion");
            var persisted = registry.LoadState();
            persisted.Installs.Should().ContainSingle(e => e.BaseName == "PluginA" && e.DirectoryName == "PluginA@v1.0");
            persisted.PendingDeletes.Should().NotContain("PluginA@v1.0");
        }
        finally
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(existing, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    /// <summary>
    /// Install async should remove a partially copied backup when backing up the registered directory fails.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldRemovePartialBackup_WhenBackupCopyFails()
    {
        using var workspace = new TempWorkspace();
        var existing = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v1.0")).FullName;
        var lockedFile = Path.Combine(existing, "PluginA.dll");
        await File.WriteAllTextAsync(lockedFile, "original");
        await File.WriteAllTextAsync(Path.Combine(existing, "other.txt"), "content");
        var registry = CreateRegistry(workspace);
        var state = registry.LoadState();
        state.Installs.Add(new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "src" });
        registry.SaveState(state);
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var backupRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-backup");
        var backupsBefore = Directory.Exists(backupRoot) ? Directory.GetDirectories(backupRoot) : [];
        await using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);
        var sut = CreateInstaller(workspace, new SuccessfulPluginManager());

        var act = () => sut.InstallAsync([incomingPlugin], new PluginPackageInstallOptions("v1.0", "src"));

        var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
        error.Which.Status.Should().Be(PluginSourceReleaseStatus.InstallFailed);
        var backupsAfter = Directory.Exists(backupRoot) ? Directory.GetDirectories(backupRoot) : [];
        backupsAfter.Should().BeEquivalentTo(backupsBefore, "a failed backup must not leave an orphaned temp directory");
    }

    private static PluginPackageInstaller CreateInstaller(TempWorkspace workspace, IPluginManager pluginManager)
        => CreateInstaller(workspace, pluginManager, CreateRegistry(workspace));

    private static PluginPackageInstaller CreateInstaller(TempWorkspace workspace, IPluginManager pluginManager, IPluginDirectoryRegistry directoryRegistry)
        => new(new TestHostEnvironment(workspace.Root), pluginManager, directoryRegistry, NullLogger<PluginPackageInstaller>.Instance);

    private sealed class FailingPluginManager : IPluginManager
    {
        public int InitializeCalls { get; private set; }

        public Task InitializeAsync(CancellationToken ct = default)
        {
            InitializeCalls++;
            if (InitializeCalls == 1)
            {
                throw new InvalidOperationException("reload failed");
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginImportHandler>>([]);
    }

    private sealed class SuccessfulPluginManager : IPluginManager
    {
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginImportHandler>>([]);
    }

    private sealed class RecordingDirectoryRegistry(IPluginDirectoryRegistry inner) : IPluginDirectoryRegistry
    {
        public List<PluginInstallState> SavedStates { get; } = [];

        public bool IsManagedRoot(string pluginRoot) => inner.IsManagedRoot(pluginRoot);

        public PluginInstallState LoadState() => inner.LoadState();

        public void SaveState(PluginInstallState state)
        {
            SavedStates.Add(state);
            inner.SaveState(state);
        }

        public string CreateVersionedDirectoryName(string baseName, string releaseTag) => inner.CreateVersionedDirectoryName(baseName, releaseTag);

        public bool TryParseVersionedDirectoryName(string directoryName, out string baseName, out string releaseTag)
            => inner.TryParseVersionedDirectoryName(directoryName, out baseName, out releaseTag);

        public IReadOnlySet<string> GetExcludedDirectoryNames(string pluginRoot, PluginInstallState state) => inner.GetExcludedDirectoryNames(pluginRoot, state);

        public string? SelectEffectiveDirectory(string baseName, IReadOnlyList<string> directoryNames, PluginInstallState state)
            => inner.SelectEffectiveDirectory(baseName, directoryNames, state);

        public void MarkPendingDeletion(PluginInstallState state, string directoryName) => inner.MarkPendingDeletion(state, directoryName);

        public void CleanupMarkedDirectories(PluginInstallState state) => inner.CleanupMarkedDirectories(state);
    }
}
