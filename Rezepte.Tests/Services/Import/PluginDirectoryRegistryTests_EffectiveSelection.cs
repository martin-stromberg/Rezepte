using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for the effective directory selection of the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_EffectiveSelection : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Select effective directory should return the registered directory when it exists on disk.
    /// </summary>
    [Fact]
    public void SelectEffectiveDirectory_ShouldReturnRegisteredDirectory_WhenItExistsOnDisk()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var candidates = new List<string> { "PluginA@v0.9", "PluginA@v1.0" };
        var state = new PluginInstallState
        {
            Installs =
            [
                new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v0.9", ReleaseTag = "v0.9", SourceKey = null }
            ]
        };

        sut.SelectEffectiveDirectory("PluginA", candidates, state).Should().Be("PluginA@v0.9");
    }

    /// <summary>
    /// Select effective directory should return the highest version when no install is registered.
    /// </summary>
    [Fact]
    public void SelectEffectiveDirectory_ShouldReturnHighestVersion_WhenNoInstallIsRegistered()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var candidates = new List<string> { "PluginA@v0.9", "PluginA@v1.0" };

        sut.SelectEffectiveDirectory("PluginA", candidates, new PluginInstallState()).Should().Be("PluginA@v1.0");
    }

    /// <summary>
    /// Select effective directory should return the highest version when the registered directory is missing on disk.
    /// </summary>
    [Fact]
    public void SelectEffectiveDirectory_ShouldReturnHighestVersion_WhenRegisteredDirectoryIsMissing()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var candidates = new List<string> { "PluginA@v0.9", "PluginA@v1.0" };
        var state = new PluginInstallState
        {
            Installs =
            [
                new PluginInstallEntry { BaseName = "PluginA", DirectoryName = "PluginA@v9.9", ReleaseTag = "v9.9", SourceKey = null }
            ]
        };

        sut.SelectEffectiveDirectory("PluginA", candidates, state).Should().Be("PluginA@v1.0");
    }
}
