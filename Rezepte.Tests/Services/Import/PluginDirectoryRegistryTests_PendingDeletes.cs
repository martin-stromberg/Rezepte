using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for the cleanup of directories marked for deletion in the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_PendingDeletes : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Cleanup marked directories should delete a pending directory and clear its entry.
    /// </summary>
    [Fact]
    public void CleanupMarkedDirectories_ShouldDeleteMarkedDirectory()
    {
        using var workspace = new TempWorkspace();
        var deletable = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Old@v0.9")).FullName;
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            PendingDeletes = ["Old@v0.9"]
        };

        sut.CleanupMarkedDirectories(state);

        Directory.Exists(deletable).Should().BeFalse();
        state.PendingDeletes.Should().BeEmpty();
        sut.LoadState().PendingDeletes.Should().BeEmpty();
    }

    /// <summary>
    /// Cleanup marked directories should keep a locked directory and retain its pending entry.
    /// </summary>
    [Fact]
    public async Task CleanupMarkedDirectories_ShouldKeepLockedDirectoryAndRetainPendingEntry()
    {
        using var workspace = new TempWorkspace();
        var lockedDirectory = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Locked@v0.8")).FullName;
        var lockedFile = Path.Combine(lockedDirectory, "file.txt");
        await File.WriteAllTextAsync(lockedFile, "content");
        await using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            PendingDeletes = ["Locked@v0.8"]
        };

        sut.CleanupMarkedDirectories(state);

        Directory.Exists(lockedDirectory).Should().BeTrue();
        state.PendingDeletes.Should().ContainSingle().Which.Should().Be("Locked@v0.8");
        sut.LoadState().PendingDeletes.Should().ContainSingle().Which.Should().Be("Locked@v0.8");
    }

    /// <summary>
    /// Cleanup marked directories should remove the pending entry when the directory is missing.
    /// </summary>
    [Fact]
    public void CleanupMarkedDirectories_ShouldRemovePendingEntry_WhenDirectoryMissing()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            PendingDeletes = ["Gone@v0.7"]
        };

        sut.CleanupMarkedDirectories(state);

        state.PendingDeletes.Should().BeEmpty();
        sut.LoadState().PendingDeletes.Should().BeEmpty();
    }

    /// <summary>
    /// Cleanup marked directories should ignore entries that do not name a direct child of the plugin root.
    /// </summary>
    /// <param name="directoryName">The directory name parameter.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("Sub\\Child@v1.0")]
    [InlineData("..\\Outside@v1.0")]
    [InlineData("a:b")]
    [InlineData("Dir@v1.0\0")]
    public void CleanupMarkedDirectories_ShouldNotDeleteOutsidePluginRoot_WhenEntryIsInvalid(string directoryName)
    {
        using var workspace = new TempWorkspace();
        var marker = Path.Combine(workspace.PluginRoot, "keep.txt");
        File.WriteAllText(marker, "content");
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            PendingDeletes = [directoryName]
        };

        sut.CleanupMarkedDirectories(state);

        File.Exists(marker).Should().BeTrue("an invalid pending entry must never reach outside the plugin root");
        Directory.Exists(workspace.Root).Should().BeTrue();
        state.PendingDeletes.Should().BeEmpty();
        sut.LoadState().PendingDeletes.Should().BeEmpty();
    }

    /// <summary>
    /// Cleanup marked directories should keep a directory that is registered as an effective installation.
    /// </summary>
    [Fact]
    public void CleanupMarkedDirectories_ShouldKeepDirectory_WhenEntryIsRegisteredInstall()
    {
        using var workspace = new TempWorkspace();
        var registered = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v1.0")).FullName;
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            Installs = [new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0" }],
            PendingDeletes = ["PluginA@v1.0"]
        };

        sut.CleanupMarkedDirectories(state);

        Directory.Exists(registered).Should().BeTrue("a registered effective installation must not be deleted");
        state.PendingDeletes.Should().BeEmpty("the inconsistent pending entry is resolved by dropping it");
        sut.LoadState().PendingDeletes.Should().BeEmpty();
    }
}
