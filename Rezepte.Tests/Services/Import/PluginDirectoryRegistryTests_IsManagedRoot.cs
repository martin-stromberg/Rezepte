using FluentAssertions;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for the managed root detection of the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_IsManagedRoot : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Is managed root should return true when the root is the managed plugin root.
    /// </summary>
    [Fact]
    public void IsManagedRoot_ShouldReturnTrue_WhenRootIsManagedPluginRoot()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        sut.IsManagedRoot(workspace.PluginRoot).Should().BeTrue();
    }

    /// <summary>
    /// Is managed root should return true when the path requires normalization.
    /// </summary>
    [Fact]
    public void IsManagedRoot_ShouldReturnTrue_WhenPathRequiresNormalization()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var nonNormalized = Path.Combine(workspace.Root, "plugins", "..", "plugins");

        sut.IsManagedRoot(nonNormalized).Should().BeTrue();
    }

    /// <summary>
    /// Is managed root should return false when the root differs from the managed plugin root.
    /// </summary>
    [Fact]
    public void IsManagedRoot_ShouldReturnFalse_WhenRootIsDifferent()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        sut.IsManagedRoot(workspace.Root).Should().BeFalse();
    }

    /// <summary>
    /// Is managed root should return false when the root is null, empty or whitespace.
    /// </summary>
    /// <param name="pluginRoot">The plugin root parameter.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsManagedRoot_ShouldReturnFalse_WhenRootIsNullOrWhiteSpace(string? pluginRoot)
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        sut.IsManagedRoot(pluginRoot!).Should().BeFalse();
    }
}
