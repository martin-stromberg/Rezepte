using System.Globalization;
using Microsoft.Data.Sqlite;
using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Application fixture for the plugin settings browser tests. Points the application's
/// <c>PluginUpdates:GitHubApiBaseUrl</c> at a local <see cref="GitHubStubServer"/> so the manual
/// update check runs without contacting the real GitHub API, and offers
/// <see cref="SeedInstalledReleaseAsync"/> to seed <c>PluginSourceReleases</c> rows directly into
/// the application's SQLite database.
/// </summary>
public sealed class PluginSettingsAppFixture : RezepteAppFixture
{
    private readonly GitHubStubServer _gitHubStub = GitHubStubServer.Start();

    /// <summary>
    /// Gets the local GitHub API stub the application under test is pointed at.
    /// </summary>
    public GitHubStubServer GitHubStub => _gitHubStub;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string?> GetEnvironmentOverrides()
    {
        return new Dictionary<string, string?>
        {
            ["PluginUpdates__GitHubApiBaseUrl"] = _gitHubStub.BaseAddress
        };
    }

    /// <summary>
    /// Stops the application process first, then the GitHub stub server, so a still-running
    /// application cannot issue requests against an already stopped stub.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _gitHubStub.Dispose();
    }

    /// <summary>
    /// Inserts a <c>PluginSourceReleases</c> row with status <c>Installed</c> for the plugin
    /// source identified by owner and repository name. The update service treats a release as
    /// already installed when a row with the same release tag and asset id exists, which lets
    /// the browser tests reach the <c>NoUpdate(alreadyInstalled)</c> path without a package
    /// download.
    /// </summary>
    /// <param name="owner">The repository owner of the plugin source.</param>
    /// <param name="repository">The repository name of the plugin source.</param>
    /// <param name="releaseTag">The release tag to seed.</param>
    /// <param name="gitHubReleaseId">The GitHub release id to seed.</param>
    /// <param name="assetId">The asset id to seed; must match the stub's asset id.</param>
    /// <param name="assetName">The asset file name to seed.</param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    public async Task SeedInstalledReleaseAsync(
        string owner,
        string repository,
        string releaseTag,
        long gitHubReleaseId,
        long assetId,
        string assetName = "plugin.zip")
    {
        var databasePath = DatabasePath
            ?? throw new InvalidOperationException("The application fixture does not expose a database path yet.");

        await using var connection = new SqliteConnection($"Data Source={databasePath};Default Timeout=10");
        await connection.OpenAsync();

        string? sourceId;
        await using (var find = connection.CreateCommand())
        {
            find.CommandText = "SELECT Id FROM PluginSources WHERE Owner = $owner AND Repository = $repository";
            find.Parameters.AddWithValue("$owner", owner);
            find.Parameters.AddWithValue("$repository", repository);
            sourceId = (string?)await find.ExecuteScalarAsync();
        }

        if (sourceId is null)
        {
            throw new InvalidOperationException(
                $"No plugin source '{owner}/{repository}' exists in the test database. Create it through the UI first.");
        }

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO PluginSourceReleases
                (Id, PluginSourceId, ReleaseTag, GitHubReleaseId, AssetId, AssetName, Status, Error,
                 CreatedAt, DownloadedAt, ValidatedAt, InstalledAt, ReloadStatus, ReloadedAt, ReloadError)
            VALUES
                ($id, $sourceId, $releaseTag, $gitHubReleaseId, $assetId, $assetName, $status, NULL,
                 $now, $now, $now, $now, $reloadStatus, $now, NULL)
            """;
        insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        insert.Parameters.AddWithValue("$sourceId", sourceId);
        insert.Parameters.AddWithValue("$releaseTag", releaseTag);
        insert.Parameters.AddWithValue("$gitHubReleaseId", gitHubReleaseId);
        insert.Parameters.AddWithValue("$assetId", assetId);
        insert.Parameters.AddWithValue("$assetName", assetName);
        insert.Parameters.AddWithValue("$status", PluginSourceReleaseStatus.Installed);
        insert.Parameters.AddWithValue("$reloadStatus", PluginSourceReleaseStatus.Installed);
        insert.Parameters.AddWithValue("$now", now);
        await insert.ExecuteNonQueryAsync();
    }
}
