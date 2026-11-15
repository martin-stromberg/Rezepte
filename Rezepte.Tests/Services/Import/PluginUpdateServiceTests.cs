using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the plugin update service tests.
/// </summary>
public sealed class PluginUpdateServiceTests : PluginUpdateServiceTestsBase
{
    /// <summary>
    /// Check for updates async should ignore disabled sources.
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesAsync_ShouldIgnoreDisabledSources()
    {
        using var db = CreateDb();
        db.PluginSources.Add(new PluginSource
        {
            RepositoryUrl = "https://github.com/owner/repo",
            Owner = "owner",
            Repository = "repo",
            Enabled = false,
            TrustConfirmed = true
        });
        await db.SaveChangesAsync();
        var github = new FakeGitHubReleaseClient(new GitHubReleaseInfo(1, "v1", [new GitHubReleaseAsset(2, "plugin.zip", "https://example.invalid/plugin.zip")]));
        var sut = CreateSut(db, github);

        await sut.CheckForUpdatesAsync();

        github.LatestReleaseCalls.Should().Be(0);
    }

    /// <summary>
    /// Check for updates async should retry previously failed release version on later run.
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesAsync_ShouldRetryPreviouslyFailedReleaseVersionOnLaterRun()
    {
        using var db = CreateDb();
        var source = new PluginSource
        {
            Id = "source-1",
            RepositoryUrl = "https://github.com/owner/repo",
            Owner = "owner",
            Repository = "repo",
            Enabled = true,
            TrustConfirmed = true
        };
        db.PluginSources.Add(source);
        db.PluginSourceReleases.Add(new PluginSourceRelease
        {
            PluginSourceId = source.Id,
            ReleaseTag = "v1",
            GitHubReleaseId = 1,
            AssetId = 2,
            AssetName = "plugin.zip",
            Status = PluginSourceReleaseStatus.ValidationFailed,
            Error = "bad package"
        });
        await db.SaveChangesAsync();
        var github = new FakeGitHubReleaseClient(new GitHubReleaseInfo(1, "v1", [new GitHubReleaseAsset(2, "plugin.zip", "https://example.invalid/plugin.zip")]));
        var sut = CreateSut(db, github);

        await sut.CheckForUpdatesAsync();

        github.DownloadCalls.Should().Be(1);
        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.Installed);
        record.Error.Should().BeNull();
        record.ReloadStatus.Should().Be(PluginSourceReleaseStatus.Installed);
        record.ReloadedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Check for updates async should persist reload failure separately.
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistReloadFailureSeparately()
    {
        using var db = CreateDb();
        var source = new PluginSource
        {
            Id = "source-1",
            RepositoryUrl = "https://github.com/owner/repo",
            Owner = "owner",
            Repository = "repo",
            Enabled = true,
            TrustConfirmed = true
        };
        db.PluginSources.Add(source);
        await db.SaveChangesAsync();
        var github = new FakeGitHubReleaseClient(new GitHubReleaseInfo(1, "v1", [new GitHubReleaseAsset(2, "plugin.zip", "https://example.invalid/plugin.zip")]));
        var sut = CreateSut(db, github, new FailingPackageInstaller(PluginSourceReleaseStatus.ReloadFailed));

        await sut.CheckForUpdatesAsync();

        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.ReloadFailed);
        record.ReloadStatus.Should().Be(PluginSourceReleaseStatus.ReloadFailed);
        record.ReloadError.Should().Be("reload failed");
        record.InstalledAt.Should().BeNull();
    }

    /// <summary>
    /// Check for updates async should pass the release tag and source key to the installer.
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPassReleaseTagAndSourceKey()
    {
        using var db = CreateDb();
        db.PluginSources.Add(new PluginSource
        {
            Id = "source-1",
            RepositoryUrl = "https://github.com/owner/repo",
            Owner = "owner",
            Repository = "repo",
            Enabled = true,
            TrustConfirmed = true
        });
        await db.SaveChangesAsync();
        var github = new FakeGitHubReleaseClient(new GitHubReleaseInfo(1, "v9.9", [new GitHubReleaseAsset(2, "plugin.zip", "https://example.invalid/plugin.zip")]));
        var installer = new FakePackageInstaller();
        var sut = CreateSut(db, github, installer);

        await sut.CheckForUpdatesAsync();

        installer.LastOptions.Should().NotBeNull();
        installer.LastOptions!.ReleaseTag.Should().Be("v9.9");
        installer.LastOptions.SourceKey.Should().Be("source-1");
    }
}
