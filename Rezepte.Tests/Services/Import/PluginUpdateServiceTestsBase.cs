using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Base class for <see cref="PluginUpdateService"/> tests providing an in-memory database,
/// plugin source fixtures and fake collaborators.
/// </summary>
public abstract class PluginUpdateServiceTestsBase
{
    /// <summary>
    /// Creates a new in-memory database context.
    /// </summary>
    /// <returns>The result.</returns>
    protected static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    /// <summary>
    /// Adds a plugin source fixture to the database and saves it.
    /// </summary>
    /// <param name="db">The db parameter.</param>
    /// <param name="id">The id parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <param name="trustConfirmed">The trust confirmed parameter.</param>
    /// <param name="isPrivate">The is private parameter.</param>
    /// <param name="secretName">The secret name parameter.</param>
    /// <returns>The result.</returns>
    protected static PluginSource AddSource(
        RezepteDbContext db,
        string id = "source-1",
        bool enabled = true,
        bool trustConfirmed = true,
        bool isPrivate = false,
        string? secretName = null)
    {
        var source = new PluginSource
        {
            Id = id,
            RepositoryUrl = $"https://github.com/owner/{id}",
            Owner = "owner",
            Repository = id,
            Enabled = enabled,
            TrustConfirmed = trustConfirmed,
            IsPrivate = isPrivate,
            SecretName = secretName
        };
        db.PluginSources.Add(source);
        db.SaveChanges();
        return source;
    }

    /// <summary>
    /// Creates the plugin update service under test.
    /// </summary>
    /// <param name="db">The db parameter.</param>
    /// <param name="gitHubReleaseClient">The git hub release client parameter.</param>
    /// <param name="installer">The installer parameter.</param>
    /// <param name="secretStore">The secret store parameter.</param>
    /// <returns>The result.</returns>
    protected static PluginUpdateService CreateSut(
        RezepteDbContext db,
        IGitHubReleaseClient gitHubReleaseClient,
        IPluginPackageInstaller? installer = null,
        ISystemSecretStore? secretStore = null)
    {
        return new PluginUpdateService(
            db,
            gitHubReleaseClient,
            secretStore ?? new FakeSecretStore(),
            new FakePackageValidator(),
            installer ?? new FakePackageInstaller(),
            NullLogger<PluginUpdateService>.Instance);
    }

    /// <summary>
    /// Creates a release fixture with a single zip asset.
    /// </summary>
    /// <param name="tagName">The tag name parameter.</param>
    /// <param name="releaseId">The release id parameter.</param>
    /// <param name="assetId">The asset id parameter.</param>
    /// <returns>The result.</returns>
    protected static GitHubReleaseInfo CreateRelease(string tagName = "v1", long releaseId = 1, long assetId = 2)
        => new(releaseId, tagName, [new GitHubReleaseAsset(assetId, "plugin.zip", "https://example.invalid/plugin.zip")]);

    /// <summary>
    /// GitHub release client that returns a fixed release.
    /// </summary>
    /// <param name="release">The release parameter.</param>
    /// <returns>The result.</returns>
    protected class FakeGitHubReleaseClient(GitHubReleaseInfo? release) : IGitHubReleaseClient
    {
        /// <summary>
        /// Gets the number of latest release calls.
        /// </summary>
        /// <returns>The result.</returns>
        public int LatestReleaseCalls { get; private set; }

        /// <summary>
        /// Gets the number of download calls.
        /// </summary>
        /// <returns>The result.</returns>
        public int DownloadCalls { get; private set; }

        /// <inheritdoc />
        public Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default)
        {
            LatestReleaseCalls++;
            return Task.FromResult(release);
        }

        /// <inheritdoc />
        public Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default)
        {
            DownloadCalls++;
            File.WriteAllText(targetPath, "zip");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// GitHub release client that throws on the latest release call.
    /// </summary>
    /// <param name="exception">The exception parameter.</param>
    /// <returns>The result.</returns>
    protected sealed class ThrowingGitHubReleaseClient(Exception exception) : IGitHubReleaseClient
    {
        /// <inheritdoc />
        public Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default)
        {
            throw exception;
        }

        /// <inheritdoc />
        public Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    /// <summary>
    /// GitHub release client that blocks in <see cref="GetLatestReleaseAsync"/> until released.
    /// </summary>
    protected sealed class BlockingGitHubReleaseClient : IGitHubReleaseClient
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<GitHubReleaseInfo?> _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets a task that completes once <see cref="GetLatestReleaseAsync"/> was entered.
        /// </summary>
        /// <returns>The result.</returns>
        public Task Entered => _entered.Task;

        /// <summary>
        /// Gets the number of latest release calls.
        /// </summary>
        /// <returns>The result.</returns>
        public int LatestReleaseCalls { get; private set; }

        /// <summary>
        /// Releases the blocked latest release call.
        /// </summary>
        /// <param name="release">The release parameter.</param>
        public void Release(GitHubReleaseInfo? release) => _gate.TrySetResult(release);

        /// <inheritdoc />
        public async Task<GitHubReleaseInfo?> GetLatestReleaseAsync(GitHubRepository repository, string? personalAccessToken, CancellationToken ct = default)
        {
            LatestReleaseCalls++;
            _entered.TrySetResult();
            return await _gate.Task.WaitAsync(ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task DownloadAssetAsync(GitHubReleaseAsset asset, string targetPath, string? personalAccessToken, CancellationToken ct = default)
        {
            File.WriteAllText(targetPath, "zip");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Secret store that returns a configurable token.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <returns>The result.</returns>
    protected sealed class FakeSecretStore(string? token = "token") : ISystemSecretStore
    {
        /// <inheritdoc />
        public Task StoreAsync(string name, string secret, CancellationToken ct = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<string?> GetAsync(string name, CancellationToken ct = default) => Task.FromResult(token);

        /// <inheritdoc />
        public Task DeleteAsync(string name, CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Package validator that always succeeds.
    /// </summary>
    protected sealed class FakePackageValidator : IPluginPackageValidator
    {
        /// <inheritdoc />
        public Task<PluginPackageValidationResult> ValidateAsync(string zipPath, CancellationToken ct = default)
        {
            var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "rezepte-update-tests", Guid.NewGuid().ToString("N"))).FullName;
            return Task.FromResult(new PluginPackageValidationResult(true, null, root, [root], []));
        }
    }

    /// <summary>
    /// Package installer that records the install options.
    /// </summary>
    protected class FakePackageInstaller : IPluginPackageInstaller
    {
        /// <summary>
        /// Gets the options of the last install call.
        /// </summary>
        /// <returns>The result.</returns>
        public PluginPackageInstallOptions? LastOptions { get; private set; }

        /// <inheritdoc />
        public Task InstallAsync(IReadOnlyList<string> pluginDirectories, PluginPackageInstallOptions options, CancellationToken ct = default)
        {
            LastOptions = options;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Package installer that fails with the given release status.
    /// </summary>
    /// <param name="status">The status parameter.</param>
    /// <returns>The result.</returns>
    protected sealed class FailingPackageInstaller(string status) : IPluginPackageInstaller
    {
        /// <inheritdoc />
        public Task InstallAsync(IReadOnlyList<string> pluginDirectories, PluginPackageInstallOptions options, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pluginDirectories);
            throw new PluginPackageInstallException(status, "reload failed", new InvalidOperationException("reload failed"));
        }
    }
}
