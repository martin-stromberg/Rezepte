using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for <see cref="PluginUpdateService.CheckSourceForUpdatesAsync"/> precondition rejections.
/// </summary>
public sealed class PluginUpdateServiceTests_Preconditions : PluginUpdateServiceTestsBase
{
    /// <summary>
    /// Check source for updates async should return failed for unknown source.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedForUnknownSource()
    {
        using var db = CreateDb();
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync("missing-source");

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Plugin source was not found.");
        github.LatestReleaseCalls.Should().Be(0);
    }

    /// <summary>
    /// Check source for updates async should return failed for disabled source.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedForDisabledSource()
    {
        using var db = CreateDb();
        var source = AddSource(db, enabled: false);
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.Error.Should().Be("Plugin source is disabled.");
        github.LatestReleaseCalls.Should().Be(0);
        source.LastCheckedAt.Should().BeNull();
    }

    /// <summary>
    /// Check source for updates async should return failed for unconfirmed source.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedForUnconfirmedSource()
    {
        using var db = CreateDb();
        var source = AddSource(db, trustConfirmed: false);
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.Error.Should().Be("Plugin source is not confirmed as trusted.");
        github.LatestReleaseCalls.Should().Be(0);
        source.LastCheckedAt.Should().BeNull();
    }
}
