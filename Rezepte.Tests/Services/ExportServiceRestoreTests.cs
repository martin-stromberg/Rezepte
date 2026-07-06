using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class ExportServiceRestoreTests
{
    [Fact]
    public async Task RestoreFromZipAsync_ShouldSkipImportedUser_WhenUsernameAlreadyExists()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string adminUserId = "current-admin";
        db.Users.Add(new Rezepte.Web.Entities.User
        {
            Id = adminUserId,
            Username = "admin",
            Email = "admin@target.local",
            PasswordHash = "hash",
            IsAdmin = true
        });
        await db.SaveChangesAsync();

        await using var zip = CreateRestoreZip(new ExportRootDto
        {
            ExportedAt = DateTime.UtcNow,
            Users =
            [
                new ExportUserDto
                {
                    Id = "source-admin",
                    UserName = "admin",
                    Email = "admin@source.local",
                    IsAdmin = true
                }
            ],
            Cookbooks =
            [
                new ExportCookbookDto
                {
                    Id = "source-cookbook",
                    UserId = "source-admin",
                    Title = "Importiertes Kochbuch"
                }
            ],
            Recipes = []
        });

        var sut = new ExportService(db, NullLogger<ExportService>.Instance);

        await sut.RestoreFromZipAsync(zip, adminUserId);

        var users = await db.Users.AsNoTracking().ToListAsync();
        users.Should().ContainSingle();
        users.Single().Id.Should().Be(adminUserId);

        var cookbook = await db.Cookbooks.AsNoTracking().SingleAsync();
        cookbook.UserId.Should().Be(adminUserId);
    }

    private static MemoryStream CreateRestoreZip(ExportRootDto root)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("recipes.json");
            using var entryStream = entry.Open();
            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            entryStream.Write(bytes);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
