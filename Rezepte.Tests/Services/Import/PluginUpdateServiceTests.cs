using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the plugin update service tests.
/// </summary>
public sealed class PluginUpdateServiceTests
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

    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private static PluginUpdateService CreateSut(RezepteDbContext db, FakeGitHubReleaseClient github, IPluginPackageInstaller? installer = null)
    {
        return new PluginUpdateService(
            db,
            github,
            new FakeSecretStore(),
            new FakePackageValidator(),
            installer ?? new FakePackageInstaller(),
            NullLogger<PluginUpdateService>.Instance);
    }

    private sealed class FakeGitHubReleaseClient(GitHubReleaseInfo? release) : IGitHubReleaseClient
    {
        public int LatestReleaseCalls { get; private set; }
        public int DownloadCalls { get; private set; }

        public Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default)
        {
            LatestReleaseCalls++;
            return Task.FromResult(release);
        }

        public Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default)
        {
            DownloadCalls++;
            File.WriteAllText(targetPath, "zip");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecretStore : ISystemSecretStore
    {
        public Task StoreAsync(string name, string secret, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string?> GetAsync(string name, CancellationToken ct = default) => Task.FromResult<string?>("token");
        public Task DeleteAsync(string name, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePackageValidator : IPluginPackageValidator
    {
        public Task<PluginPackageValidationResult> ValidateAsync(string zipPath, CancellationToken ct = default)
        {
            var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "rezepte-update-tests", Guid.NewGuid().ToString("N"))).FullName;
            return Task.FromResult(new PluginPackageValidationResult(true, null, root, [root], []));
        }
    }

    private sealed class FakePackageInstaller : IPluginPackageInstaller
    {
        public Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FailingPackageInstaller(string status) : IPluginPackageInstaller
    {
        public Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pluginDirectories);
            throw new PluginPackageInstallException(status, "reload failed", new InvalidOperationException("reload failed"));
        }
    }
}
