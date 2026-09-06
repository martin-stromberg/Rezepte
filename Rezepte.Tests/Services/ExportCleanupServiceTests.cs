using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the export cleanup service tests.
/// </summary>
public sealed class ExportCleanupServiceTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), $"rezepte-cleanup-{Guid.NewGuid():N}");
    private readonly RezepteDbContext _db;
    private readonly ExportJobFileStore _fileStore;
    private readonly ExportCleanupService _sut;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ExportCleanupServiceTests()
    {
        Directory.CreateDirectory(_contentRoot);

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new RezepteDbContext(options);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.ContentRootPath).Returns(_contentRoot);
        _fileStore = new ExportJobFileStore(environment.Object);

        _sut = new ExportCleanupService(_db, _fileStore, NullLogger<ExportCleanupService>.Instance);
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    /// <summary>
    /// Get settings async should return defaults when nothing stored.
    /// </summary>
    [Fact]
    public async Task GetSettingsAsync_ShouldReturnDefaults_WhenNothingStored()
    {
        var settings = await _sut.GetSettingsAsync();

        settings.CleanupTime.Should().Be(ExportCleanupService.DefaultCleanupTime);
        settings.LastRunAt.Should().BeNull();
    }

    /// <summary>
    /// Set cleanup time async should persist time.
    /// </summary>
    [Fact]
    public async Task SetCleanupTimeAsync_ShouldPersistTime()
    {
        await _sut.SetCleanupTimeAsync(new TimeOnly(22, 30));

        var settings = await _sut.GetSettingsAsync();
        settings.CleanupTime.Should().Be(new TimeOnly(22, 30));
        (await _db.AppSettings.FindAsync(ExportCleanupService.CleanupTimeKey))!.Value.Should().Be("22:30");
    }

    /// <summary>
    /// Is cleanup due should be true when never run.
    /// </summary>
    [Fact]
    public void IsCleanupDue_ShouldBeTrue_WhenNeverRun()
    {
        var settings = new ExportCleanupSettings(new TimeOnly(3, 0), null);

        ExportCleanupService.IsCleanupDue(settings, new DateTimeOffset(2026, 9, 5, 1, 0, 0, TimeSpan.Zero)).Should().BeTrue();
    }

    /// <summary>
    /// Is cleanup due should be false when already run after last scheduled occurrence.
    /// </summary>
    [Fact]
    public void IsCleanupDue_ShouldBeFalse_WhenAlreadyRunAfterLastScheduledOccurrence()
    {
        var lastRun = new DateTimeOffset(2026, 9, 5, 3, 0, 5, TimeSpan.Zero);
        var settings = new ExportCleanupSettings(new TimeOnly(3, 0), lastRun);

        ExportCleanupService.IsCleanupDue(settings, new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero)).Should().BeFalse();
        ExportCleanupService.IsCleanupDue(settings, new DateTimeOffset(2026, 9, 6, 2, 59, 0, TimeSpan.Zero)).Should().BeFalse();
    }

    /// <summary>
    /// Is cleanup due should be true when scheduled time passed since last run.
    /// </summary>
    [Fact]
    public void IsCleanupDue_ShouldBeTrue_WhenScheduledTimePassedSinceLastRun()
    {
        var lastRun = new DateTimeOffset(2026, 9, 5, 3, 0, 5, TimeSpan.Zero);
        var settings = new ExportCleanupSettings(new TimeOnly(3, 0), lastRun);

        ExportCleanupService.IsCleanupDue(settings, new DateTimeOffset(2026, 9, 6, 3, 0, 0, TimeSpan.Zero)).Should().BeTrue();
    }

    /// <summary>
    /// Is cleanup due should catch up missed run when application was offline at scheduled time.
    /// </summary>
    [Fact]
    public void IsCleanupDue_ShouldCatchUpMissedRun_WhenApplicationWasOfflineAtScheduledTime()
    {
        // Last run two days ago; the application was down yesterday at 03:00 and is started
        // today at 01:00, before today's occurrence. Yesterday's occurrence must be caught up.
        var lastRun = new DateTimeOffset(2026, 9, 3, 3, 0, 1, TimeSpan.Zero);
        var settings = new ExportCleanupSettings(new TimeOnly(3, 0), lastRun);

        ExportCleanupService.IsCleanupDue(settings, new DateTimeOffset(2026, 9, 5, 1, 0, 0, TimeSpan.Zero)).Should().BeTrue();
    }

    /// <summary>
    /// Get last scheduled occurrence should use yesterday when time not yet reached today.
    /// </summary>
    [Fact]
    public void GetLastScheduledOccurrence_ShouldUseYesterday_WhenTimeNotYetReachedToday()
    {
        var now = new DateTimeOffset(2026, 9, 5, 1, 0, 0, TimeSpan.FromHours(2));

        var occurrence = ExportCleanupService.GetLastScheduledOccurrence(new TimeOnly(3, 0), now);

        occurrence.Should().Be(new DateTimeOffset(2026, 9, 4, 3, 0, 0, TimeSpan.FromHours(2)));
    }

    /// <summary>
    /// Run cleanup async should delete expired files and records but keep recent ones.
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_ShouldDeleteExpiredFilesAndRecords_ButKeepRecentOnes()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = AddExportFile("expired.zip", now.UtcDateTime.AddDays(-1).AddMinutes(-1));
        var recent = AddExportFile("recent.zip", now.UtcDateTime.AddHours(-23));
        await _db.SaveChangesAsync();

        var result = await _sut.RunCleanupAsync(now);

        result.DeletedFiles.Should().Be(1);
        result.DeletedRecords.Should().Be(1);
        File.Exists(_fileStore.GetPathForFileName(expired.FileName)).Should().BeFalse();
        File.Exists(_fileStore.GetPathForFileName(recent.FileName)).Should().BeTrue();
        (await _db.UserExportFiles.ToListAsync()).Should().ContainSingle(f => f.Id == recent.Id);
    }

    /// <summary>
    /// Run cleanup async should remove record when file already missing.
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_ShouldRemoveRecord_WhenFileAlreadyMissing()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = AddExportFile("missing.zip", now.UtcDateTime.AddDays(-2), createFile: false);
        await _db.SaveChangesAsync();

        var result = await _sut.RunCleanupAsync(now);

        result.DeletedFiles.Should().Be(0);
        result.DeletedRecords.Should().Be(1);
        (await _db.UserExportFiles.FindAsync(expired.Id)).Should().BeNull();
    }

    /// <summary>
    /// Run cleanup async should delete orphaned archives older than one day.
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_ShouldDeleteOrphanedArchivesOlderThanOneDay()
    {
        var now = DateTimeOffset.UtcNow;
        var orphanOld = _fileStore.GetPathForFileName("orphan-old.zip");
        File.WriteAllText(orphanOld, "x");
        File.SetLastWriteTimeUtc(orphanOld, now.UtcDateTime.AddDays(-3));
        var orphanNew = _fileStore.GetPathForFileName("orphan-new.zip");
        File.WriteAllText(orphanNew, "x");

        var result = await _sut.RunCleanupAsync(now);

        result.DeletedFiles.Should().Be(1);
        File.Exists(orphanOld).Should().BeFalse();
        File.Exists(orphanNew).Should().BeTrue();
    }

    /// <summary>
    /// Run cleanup async should record last run so cleanup is no longer due.
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_ShouldRecordLastRun_SoCleanupIsNoLongerDue()
    {
        var now = new DateTimeOffset(2026, 9, 5, 3, 0, 0, TimeSpan.Zero);
        (await _sut.IsCleanupDueAsync(now)).Should().BeTrue();

        var result = await _sut.RunCleanupAsync(now);

        result.RunAt.Should().Be(now);
        (await _sut.GetSettingsAsync()).LastRunAt.Should().Be(now);
        (await _sut.IsCleanupDueAsync(now.AddHours(1))).Should().BeFalse();
        (await _sut.IsCleanupDueAsync(now.AddDays(1))).Should().BeTrue();
    }

    private UserExportFile AddExportFile(string fileName, DateTime createdAt, bool createFile = true)
    {
        if (createFile)
        {
            File.WriteAllText(_fileStore.GetPathForFileName(fileName), "zip");
        }

        var file = new UserExportFile
        {
            UserId = "user-1",
            FileName = fileName,
            Size = 3,
            CreatedAt = createdAt
        };
        _db.UserExportFiles.Add(file);
        return file;
    }
}
