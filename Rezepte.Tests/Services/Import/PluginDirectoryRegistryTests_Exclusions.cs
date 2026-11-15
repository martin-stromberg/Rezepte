using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for the discovery exclusion set computed by the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_Exclusions : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Get excluded directory names should exclude a directory marked for deletion.
    /// </summary>
    [Fact]
    public void GetExcludedDirectoryNames_ShouldExcludePendingDeleteDirectory()
    {
        using var workspace = new TempWorkspace();
        Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Pending@v1"));
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            PendingDeletes = ["Pending@v1"]
        };

        var excluded = sut.GetExcludedDirectoryNames(workspace.PluginRoot, state);

        excluded.Should().Contain("Pending@v1");
    }

    /// <summary>
    /// Get excluded directory names should exclude the non effective versioned directory of a base name.
    /// </summary>
    [Fact]
    public void GetExcludedDirectoryNames_ShouldExcludeNonEffectiveVersionedDirectory()
    {
        using var workspace = new TempWorkspace();
        Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v0.9"));
        Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA@v1.0"));
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState
        {
            Installs =
            [
                new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v1.0", ReleaseTag = "v1.0", SourceKey = "source-1" }
            ]
        };

        var excluded = sut.GetExcludedDirectoryNames(workspace.PluginRoot, state);

        excluded.Should().Contain("PluginA@v0.9");
        excluded.Should().NotContain("PluginA@v1.0");
    }

    /// <summary>
    /// Get excluded directory names should exclude a flat directory shadowed by a versioned directory.
    /// </summary>
    [Fact]
    public void GetExcludedDirectoryNames_ShouldExcludeShadowedFlatDirectory()
    {
        using var workspace = new TempWorkspace();
        Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Flat"));
        Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Flat@v2"));
        Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Publish"));
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState();

        var excluded = sut.GetExcludedDirectoryNames(workspace.PluginRoot, state);

        excluded.Should().Contain("Flat");
        excluded.Should().NotContain(["Flat@v2", "Publish"]);
    }
}
