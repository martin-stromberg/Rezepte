using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for loading and saving the persistent install state of the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_State : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Load state should return an empty state when the file is missing.
    /// </summary>
    [Fact]
    public void LoadState_ShouldReturnEmptyState_WhenFileMissing()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        var missing = sut.LoadState();

        missing.Version.Should().Be(1);
        missing.Installs.Should().BeEmpty();
        missing.PendingDeletes.Should().BeEmpty();
    }

    /// <summary>
    /// Load state should return an empty state when the file is corrupt.
    /// </summary>
    [Fact]
    public void LoadState_ShouldReturnEmptyState_WhenFileCorrupt()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        File.WriteAllText(Path.Combine(workspace.PluginRoot, ".install-state.json"), "{ this is not valid json");

        var corrupt = sut.LoadState();

        corrupt.Installs.Should().BeEmpty();
        corrupt.PendingDeletes.Should().BeEmpty();
    }

    /// <summary>
    /// Save state should roundtrip through a new registry instance.
    /// </summary>
    [Fact]
    public void SaveState_ShouldRoundtrip()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            Installs =
            [
                new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "source-1" }
            ],
            PendingDeletes = ["PluginA@v0.9"]
        };

        sut.SaveState(state);

        var reloaded = CreateRegistry(workspace).LoadState();
        reloaded.Installs.Should().ContainSingle(e =>
            e.BaseName == "PluginA" &&
            e.DirectoryName == "PluginA@v1.0" &&
            e.ReleaseTag == "v1.0" &&
            e.SourceKey == "source-1");
        reloaded.PendingDeletes.Should().ContainSingle().Which.Should().Be("PluginA@v0.9");
    }
}
