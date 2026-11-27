using FluentAssertions;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for the versioned directory naming helpers of the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_Names : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Create versioned directory name should sanitize invalid characters.
    /// </summary>
    [Fact]
    public void CreateVersionedDirectoryName_ShouldSanitizeInvalidCharacters()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        sut.CreateVersionedDirectoryName("PluginA", "v1/2").Should().Be("PluginA@v1-2");
        sut.CreateVersionedDirectoryName("PluginA", "v1@beta").Should().Be("PluginA@v1-beta");
        sut.CreateVersionedDirectoryName("PluginA", "v1.0").Should().Be("PluginA@v1.0");
    }

    /// <summary>
    /// Try parse versioned directory name should split on the first at sign.
    /// </summary>
    /// <param name="directoryName">The directory name parameter.</param>
    /// <param name="expectedBaseName">The expected base name parameter.</param>
    /// <param name="expectedReleaseTag">The expected release tag parameter.</param>
    [Theory]
    [InlineData("Name@v1.0", "Name", "v1.0")]
    [InlineData("Name@x@y", "Name", "x@y")]
    public void TryParseVersionedDirectoryName_ShouldSplitOnFirstAt(string directoryName, string expectedBaseName, string expectedReleaseTag)
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        sut.TryParseVersionedDirectoryName(directoryName, out var baseName, out var releaseTag).Should().BeTrue();
        baseName.Should().Be(expectedBaseName);
        releaseTag.Should().Be(expectedReleaseTag);
    }

    /// <summary>
    /// Try parse versioned directory name should return false when the name is not versioned.
    /// </summary>
    /// <param name="directoryName">The directory name parameter.</param>
    [Theory]
    [InlineData("Name")]
    [InlineData("@v1")]
    [InlineData("Name@")]
    [InlineData(".work")]
    public void TryParseVersionedDirectoryName_ShouldReturnFalse_WhenNameIsNotVersioned(string directoryName)
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);

        sut.TryParseVersionedDirectoryName(directoryName, out _, out _).Should().BeFalse();
    }
}
