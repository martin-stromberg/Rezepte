using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for <see cref="PluginUpdateService.CheckSourceForUpdatesAsync"/> result mapping and status tracking.
/// </summary>
public sealed class PluginUpdateServiceTests_CheckSource : PluginUpdateServiceTestsBase
{
    /// <summary>
    /// Check source for updates async should install new release and return installed.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldInstallNewReleaseAndReturnInstalled()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Installed);
        result.IsSuccess.Should().BeTrue();
        result.ReleaseTag.Should().Be("v1");
        result.Error.Should().BeNull();
        result.AlreadyInstalled.Should().BeFalse();

        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.Installed);
        record.ReloadStatus.Should().Be(PluginSourceReleaseStatus.Installed);

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastSuccessfulReleaseTag.Should().Be("v1");
        updated.LastCheckedAt.Should().NotBeNull();
        updated.LastError.Should().BeNull();
        updated.LastErrorAt.Should().BeNull();
    }

    /// <summary>
    /// Check source for updates async should return already installed for installed release.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnAlreadyInstalledForInstalledRelease()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        db.PluginSourceReleases.Add(new PluginSourceRelease
        {
            PluginSourceId = source.Id,
            ReleaseTag = "v1",
            GitHubReleaseId = 1,
            AssetId = 2,
            AssetName = "plugin.zip",
            Status = PluginSourceReleaseStatus.Installed
        });
        await db.SaveChangesAsync();
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.NoUpdate);
        result.IsSuccess.Should().BeTrue();
        result.AlreadyInstalled.Should().BeTrue();
        result.ReleaseTag.Should().Be("v1");
        github.DownloadCalls.Should().Be(0);
    }

    /// <summary>
    /// Check source for updates async should retry previously failed release.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldRetryPreviouslyFailedRelease()
    {
        using var db = CreateDb();
        var source = AddSource(db);
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
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Installed);
        result.ReleaseTag.Should().Be("v1");
        github.DownloadCalls.Should().Be(1);
        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.Installed);
        record.Error.Should().BeNull();
    }

    /// <summary>
    /// Check source for updates async should return failed when no release found.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedWhenNoReleaseFound()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        var github = new FakeGitHubReleaseClient(null);
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No published GitHub release was found.");

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastError.Should().Be("No published GitHub release was found.");
        updated.LastErrorAt.Should().NotBeNull();
        updated.LastCheckedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Check source for updates async should return failed when release has no zip asset.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedWhenReleaseHasNoZipAsset()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        var github = new FakeGitHubReleaseClient(new GitHubReleaseInfo(1, "v1", [new GitHubReleaseAsset(2, "notes.txt", "https://example.invalid/notes.txt")]));
        var sut = CreateSut(db, github);

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.Error.Should().Be("Latest release has no ZIP asset.");

        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.ValidationFailed);
        record.Error.Should().Be("Latest release has no ZIP asset.");

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastError.Should().Be("Latest release has no ZIP asset.");
        github.DownloadCalls.Should().Be(0);
    }

    /// <summary>
    /// Check source for updates async should return failed when private source has no pat.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedWhenPrivateSourceHasNoPat()
    {
        using var db = CreateDb();
        var source = AddSource(db, isPrivate: true, secretName: "plugin-source-source-1-pat");
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github, secretStore: new FakeSecretStore(token: null));

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.Error.Should().Be("Private source has no stored PAT.");

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastError.Should().Be("Private source has no stored PAT.");
        updated.LastErrorAt.Should().NotBeNull();
        github.LatestReleaseCalls.Should().Be(0);
    }

    /// <summary>
    /// Check source for updates async should return failed on install failure.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedOnInstallFailure()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github, new FailingPackageInstaller(PluginSourceReleaseStatus.ReloadFailed));

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("reload failed");
        result.ReleaseTag.Should().Be("v1");

        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.ReloadFailed);
        record.ReloadStatus.Should().Be(PluginSourceReleaseStatus.ReloadFailed);

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastError.Should().Be("reload failed");
        updated.LastErrorAt.Should().NotBeNull();
    }

    /// <summary>
    /// Check source for updates async should return failed on unexpected exception.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnFailedOnUnexpectedException()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        var sut = CreateSut(db, new ThrowingGitHubReleaseClient(new InvalidOperationException("github unavailable")));

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.Error.Should().Be("github unavailable");

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastError.Should().Be("github unavailable");
        updated.LastErrorAt.Should().NotBeNull();
    }

    /// <summary>
    /// Check source for updates async should return the work directory error and mark the pending
    /// release record as failed when the workspace provider cannot create the download work
    /// directory.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReturnWorkDirectoryUnavailable_WhenWorkspaceFails()
    {
        using var db = CreateDb();
        var source = AddSource(db);
        var github = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var sut = CreateSut(db, github, workspaceProvider: new ThrowingWorkspaceProvider(new PluginWorkspaceException()));

        var result = await sut.CheckSourceForUpdatesAsync(source.Id);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);
        result.Error.Should().Be(PluginUpdateCheckErrors.WorkDirectoryUnavailable);
        result.ReleaseTag.Should().Be("v1");

        var record = await db.PluginSourceReleases.SingleAsync();
        record.Status.Should().Be(PluginSourceReleaseStatus.InstallFailed,
            "a workspace failure after the pending upsert must not leave the record pending without an error");
        record.Error.Should().Be(PluginUpdateCheckErrors.WorkDirectoryUnavailable);

        var updated = await db.PluginSources.FindAsync(source.Id);
        updated!.LastError.Should().Be(PluginUpdateCheckErrors.WorkDirectoryUnavailable);
        updated.LastErrorAt.Should().NotBeNull();
        github.DownloadCalls.Should().Be(0);
    }
}
