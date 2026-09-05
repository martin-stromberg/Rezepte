using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Services.BackgroundJobs;
using Xunit;

namespace Rezepte.Tests.Services.BackgroundJobs;

/// <summary>
/// Class representing the background job persistence tests.
/// </summary>
public class BackgroundJobPersistenceTests
{
    /// <summary>
    /// Db context should persist background job.
    /// </summary>
    [Fact]
    public async Task DbContext_ShouldPersistBackgroundJob()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var job = new BackgroundJob
        {
            JobType = "export:all",
            InitiatorUserId = "admin"
        };

        db.BackgroundJobs.Add(job);
        await db.SaveChangesAsync();

        var persisted = await db.BackgroundJobs.AsNoTracking().SingleAsync();
        persisted.JobType.Should().Be("export:all");
        persisted.InitiatorUserId.Should().Be("admin");
        persisted.Status.Should().Be(BackgroundJobStatus.Pending);
    }

    /// <summary>
    /// Db context should round trip all background job statuses.
    /// </summary>
    [Fact]
    public async Task DbContext_ShouldRoundTripAllBackgroundJobStatuses()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var statuses = new[]
        {
            BackgroundJobStatus.Running,
            BackgroundJobStatus.Failed,
            BackgroundJobStatus.Cancelled
        };

        foreach (var status in statuses)
        {
            db.BackgroundJobs.Add(new BackgroundJob
            {
                JobType = $"export:{status}",
                InitiatorUserId = "admin",
                Status = status
            });
        }

        await db.SaveChangesAsync();

        var persisted = await db.BackgroundJobs.AsNoTracking().Select(j => j.Status).ToListAsync();
        persisted.Should().BeEquivalentTo(statuses);
    }

    /// <summary>
    /// Migrations should create background jobs table.
    /// </summary>
    [Fact]
    public async Task Migrations_ShouldCreateBackgroundJobsTable()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);

        db.Database.GetMigrations()
            .Should()
            .Contain("20260706090000_AddBackgroundJobsTable");

        await db.Database.MigrateAsync();

        var job = new BackgroundJob
        {
            JobType = "export:all",
            InitiatorUserId = "admin"
        };

        db.BackgroundJobs.Add(job);
        await db.SaveChangesAsync();

        (await db.BackgroundJobs.AsNoTracking().CountAsync()).Should().Be(1);
    }
}
