using System.IO.Compression;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public sealed class UpdateBackupServiceTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "rezepte-update-backup-tests", Guid.NewGuid().ToString("N"));

    public UpdateBackupServiceTests()
    {
        Directory.CreateDirectory(_contentRoot);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldWriteFinalZipAndUseConfiguredExportOptions()
    {
        var export = new RecordingExportService(CreateZipBytes("recipes.json", "{}"));
        var sut = CreateSut(export, new UpdateBackupOptions
        {
            Directory = "backups",
            RetentionCount = 5,
            IncludeImages = true,
            IncludePdf = false,
            SystemInitiatorUserId = "system-backup"
        });

        var result = await sut.CreateBackupAsync();

        File.Exists(result.FilePath).Should().BeTrue();
        Path.GetFileName(result.FilePath).Should().StartWith("update-backup-").And.EndWith(".zip");
        result.SizeBytes.Should().BeGreaterThan(0);
        Directory.EnumerateFiles(Path.Combine(_contentRoot, "backups"), "*.tmp").Should().BeEmpty();
        export.AdminUserId.Should().Be("system-backup");
        export.IncludeImages.Should().BeTrue();
        export.IncludePdf.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldApplyRetentionOnlyToUpdateBackups()
    {
        var backupDirectory = Path.Combine(_contentRoot, "backups");
        Directory.CreateDirectory(backupDirectory);
        var oldest = CreateExistingBackup(backupDirectory, "update-backup-20260101-000000-0000000Z.zip", DateTime.UtcNow.AddDays(-4));
        var middle = CreateExistingBackup(backupDirectory, "update-backup-20260102-000000-0000000Z.zip", DateTime.UtcNow.AddDays(-3));
        var newest = CreateExistingBackup(backupDirectory, "update-backup-20260103-000000-0000000Z.zip", DateTime.UtcNow.AddDays(-2));
        var unrelated = Path.Combine(backupDirectory, "manual-export.zip");
        await File.WriteAllTextAsync(unrelated, "keep");

        var sut = CreateSut(new RecordingExportService(CreateZipBytes("recipes.json", "{}")), new UpdateBackupOptions
        {
            Directory = backupDirectory,
            RetentionCount = 3
        });

        var result = await sut.CreateBackupAsync();

        File.Exists(result.FilePath).Should().BeTrue();
        File.Exists(oldest).Should().BeFalse();
        File.Exists(middle).Should().BeTrue();
        File.Exists(newest).Should().BeTrue();
        File.Exists(unrelated).Should().BeTrue();
        Directory.EnumerateFiles(backupDirectory, "update-backup-*.zip").Should().HaveCount(3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateBackupAsync_ShouldRejectMissingBackupDirectory(string directory)
    {
        var export = new RecordingExportService(CreateZipBytes("recipes.json", "{}"));
        var sut = CreateSut(export, new UpdateBackupOptions { Directory = directory, RetentionCount = 5 });

        var act = () => sut.CreateBackupAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*directory*");
        export.ExportAllCalls.Should().Be(0);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldRejectInvalidRetentionBeforeExport()
    {
        var export = new RecordingExportService(CreateZipBytes("recipes.json", "{}"));
        var sut = CreateSut(export, new UpdateBackupOptions { Directory = "backups", RetentionCount = 0 });

        var act = () => sut.CreateBackupAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*retention*");
        export.ExportAllCalls.Should().Be(0);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldNotPublishFinalBackup_WhenExportFails()
    {
        var backupDirectory = Path.Combine(_contentRoot, "backups");
        var sut = CreateSut(new FailingExportService(), new UpdateBackupOptions
        {
            Directory = backupDirectory,
            RetentionCount = 5
        });

        var act = () => sut.CreateBackupAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("export failed");
        Directory.EnumerateFiles(backupDirectory, "update-backup-*.zip").Should().BeEmpty();
        Directory.EnumerateFiles(backupDirectory, "*.tmp").Should().BeEmpty();
    }

    private UpdateBackupService CreateSut(IExportService exportService, UpdateBackupOptions options)
        => new(
            exportService,
            Options.Create(options),
            new TestHostEnvironment(_contentRoot),
            NullLogger<UpdateBackupService>.Instance);

    private static string CreateExistingBackup(string directory, string fileName, DateTime lastWriteUtc)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "backup");
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }

    private static byte[] CreateZipBytes(string entryName, string content)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        return stream.ToArray();
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private sealed class RecordingExportService(byte[] payload) : IExportService
    {
        public int ExportAllCalls { get; private set; }
        public string? AdminUserId { get; private set; }
        public bool IncludeImages { get; private set; }
        public bool IncludePdf { get; private set; }

        public Task<Stream> ExportUserAsync(string userId, bool includeImages, bool includePdf, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<Stream> ExportAllAsync(string adminUserId, bool includeImages, bool includePdf, CancellationToken ct = default)
        {
            ExportAllCalls++;
            AdminUserId = adminUserId;
            IncludeImages = includeImages;
            IncludePdf = includePdf;
            return Task.FromResult<Stream>(new MemoryStream(payload));
        }

        public Task RestoreFromZipAsync(Stream zipStream, string adminUserId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class FailingExportService : IExportService
    {
        public Task<Stream> ExportUserAsync(string userId, bool includeImages, bool includePdf, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<Stream> ExportAllAsync(string adminUserId, bool includeImages, bool includePdf, CancellationToken ct = default)
            => throw new InvalidOperationException("export failed");

        public Task RestoreFromZipAsync(Stream zipStream, string adminUserId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Rezepte.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
