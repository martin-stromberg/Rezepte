using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for marking directories for deletion in the plugin directory registry.
/// </summary>
public sealed class PluginDirectoryRegistryTests_Marking : PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Mark pending deletion should add the directory name.
    /// </summary>
    [Fact]
    public void MarkPendingDeletion_ShouldAddDirectoryName()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState();

        sut.MarkPendingDeletion(state, "PluginA@v1.0");

        state.PendingDeletes.Should().ContainSingle().Which.Should().Be("PluginA@v1.0");
    }

    /// <summary>
    /// Mark pending deletion should deduplicate entries ignoring the letter casing.
    /// </summary>
    [Fact]
    public void MarkPendingDeletion_ShouldDeduplicateIgnoringCase()
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState();

        sut.MarkPendingDeletion(state, "PluginA@v1.0");
        sut.MarkPendingDeletion(state, "plugina@V1.0");

        state.PendingDeletes.Should().ContainSingle().Which.Should().Be("PluginA@v1.0");
    }

    /// <summary>
    /// Mark pending deletion should ignore missing or blank directory names.
    /// </summary>
    /// <param name="directoryName">The directory name parameter.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkPendingDeletion_ShouldIgnoreMissingName(string? directoryName)
    {
        using var workspace = new TempWorkspace();
        var sut = CreateRegistry(workspace);
        var state = new PluginInstallState();

        sut.MarkPendingDeletion(state, directoryName!);

        state.PendingDeletes.Should().BeEmpty();
    }
}
