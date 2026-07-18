using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class PluginSourceReleaseMigrationTests
{
    [Fact]
    public async Task Migrations_ShouldAddReloadStateColumnsToExistingPluginSourceReleasesTable()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);

        db.Database.GetMigrations()
            .Should()
            .Contain("20260718131500_AddPluginSourceReleaseReloadState");

        await db.Database.MigrateAsync("20260718103846_AddPluginSources");

        var columnsBeforeReloadState = await GetPluginSourceReleaseColumnsAsync(connection);
        columnsBeforeReloadState.Should().NotContain("ReloadError");
        columnsBeforeReloadState.Should().NotContain("ReloadedAt");
        columnsBeforeReloadState.Should().NotContain("ReloadStatus");

        await db.Database.MigrateAsync();

        var columnsAfterReloadState = await GetPluginSourceReleaseColumnsAsync(connection);
        columnsAfterReloadState.Should().Contain(["ReloadError", "ReloadedAt", "ReloadStatus"]);
    }

    private static async Task<HashSet<string>> GetPluginSourceReleaseColumnsAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('PluginSourceReleases');";

        await using var reader = await command.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.Ordinal);

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
