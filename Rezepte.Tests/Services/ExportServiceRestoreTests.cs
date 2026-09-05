using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the export service restore tests.
/// </summary>
public class ExportServiceRestoreTests
{
    /// <summary>
    /// Restore from zip async should skip imported user when username already exists.
    /// </summary>
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

    /// <summary>
    /// Restore from zip async should throw when recipes json missing.
    /// </summary>
    [Fact]
    public async Task RestoreFromZipAsync_ShouldThrow_WhenRecipesJsonMissing()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        await using var zip = CreateRestoreZip(new ExportRootDto(), includeRecipesJson: false);

        var sut = new ExportService(db, NullLogger<ExportService>.Instance);

        var act = async () => await sut.RestoreFromZipAsync(zip, "admin");

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    /// <summary>
    /// Restore from zip async should throw when archive contains too many entries.
    /// </summary>
    [Fact]
    public async Task RestoreFromZipAsync_ShouldThrow_WhenArchiveContainsTooManyEntries()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var extra = new Dictionary<string, byte[]>
        {
            ["metadata.json"] = Encoding.UTF8.GetBytes("{}")
        };

        await using var zip = CreateRestoreZip(new ExportRootDto(), extra);

        var validationOptions = new RestoreValidationOptions { MaxArchiveEntries = 1 };
        var sut = new ExportService(db, NullLogger<ExportService>.Instance, null, validationOptions);

        var act = async () => await sut.RestoreFromZipAsync(zip, "admin");

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    /// <summary>
    /// Restore from zip async should throw when compression ratio exceeds limit.
    /// </summary>
    [Fact]
    public async Task RestoreFromZipAsync_ShouldThrow_WhenCompressionRatioExceedsLimit()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var extra = new Dictionary<string, byte[]>
        {
            ["images/test.txt"] = new byte[50_000]
        };

        await using var zip = CreateRestoreZip(new ExportRootDto { ExportedAt = DateTime.UtcNow }, extra);

        var validationOptions = new RestoreValidationOptions { MaxCompressionRatio = 10 };
        var sut = new ExportService(db, NullLogger<ExportService>.Instance, null, validationOptions);

        var act = async () => await sut.RestoreFromZipAsync(zip, "admin");

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    /// <summary>
    /// Restore from zip async should throw when image exceeds max uncompressed size.
    /// </summary>
    [Fact]
    public async Task RestoreFromZipAsync_ShouldThrow_WhenImageExceedsMaxUncompressedSize()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var extra = new Dictionary<string, byte[]>
        {
            ["images/test.jpg"] = new byte[20]
        };

        await using var zip = CreateRestoreZip(new ExportRootDto { ExportedAt = DateTime.UtcNow }, extra);

        var validationOptions = new RestoreValidationOptions { MaxImageUncompressedBytes = 10 };
        var sut = new ExportService(db, NullLogger<ExportService>.Instance, null, validationOptions);

        var act = async () => await sut.RestoreFromZipAsync(zip, "admin");

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    /// <summary>
    /// Restore from zip async should rollback when total image size exceeded.
    /// </summary>
    [Fact]
    public async Task RestoreFromZipAsync_ShouldRollback_WhenTotalImageSizeExceeded()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string adminUserId = "admin";
        const string existingUserId = "existing-user";
        const string existingRecipeId = "existing-recipe";

        db.Users.AddRange(
            new Rezepte.Web.Entities.User
            {
                Id = adminUserId,
                Username = "admin",
                Email = "admin@target.local",
                PasswordHash = "hash",
                IsAdmin = true
            },
            new Rezepte.Web.Entities.User
            {
                Id = existingUserId,
                Username = "existing",
                Email = "existing@target.local",
                PasswordHash = "hash",
                IsAdmin = false
            });
        db.Recipes.Add(new Rezepte.Web.Entities.Recipe
        {
            Id = existingRecipeId,
            UserId = existingUserId,
            Title = "Existing recipe",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        const string recipeId = "new-recipe";

        var extra = new Dictionary<string, byte[]>
        {
            ["images/recipe1/image01.jpg"] = new byte[5],
            ["images/recipe1/image02.jpg"] = new byte[6]
        };

        await using var zip = CreateRestoreZip(new ExportRootDto
        {
            ExportedAt = DateTime.UtcNow,
            Users =
            [
                new ExportUserDto
                {
                    Id = "new-user",
                    UserName = "new",
                    Email = "new@source.local",
                    IsAdmin = false
                }
            ],
            Cookbooks =
            [
                new ExportCookbookDto
                {
                    Id = "new-cookbook",
                    UserId = "new-user",
                    Title = "New cookbook"
                }
            ],
            Recipes =
            [
                new ExportRecipeDto
                {
                    Id = recipeId,
                    OwnerId = "new-user",
                    Title = "New recipe",
                    ImagePaths =
                    [
                        "images/recipe1/image01.jpg",
                        "images/recipe1/image02.jpg"
                    ]
                }
            ]
        }, extra);

        var validationOptions = new RestoreValidationOptions { MaxTotalImageBytes = 10 };
        var sut = new ExportService(db, NullLogger<ExportService>.Instance, null, validationOptions);

        await Assert.ThrowsAsync<InvalidDataException>(() => sut.RestoreFromZipAsync(zip, adminUserId));

        var users = await db.Users.AsNoTracking().ToListAsync();
        users.Should().Contain(u => u.Id == adminUserId);
        users.Should().Contain(u => u.Id == existingUserId);
        users.Should().NotContain(u => u.Id == "new-user");

        var recipes = await db.Recipes.AsNoTracking().ToListAsync();
        recipes.Should().ContainSingle(r => r.Id == existingRecipeId);
        recipes.Should().NotContain(r => r.Id == recipeId);

        var cookbooks = await db.Cookbooks.AsNoTracking().ToListAsync();
        cookbooks.Should().BeEmpty();
    }

    /// <summary>
    /// Restore from zip async should restore recipe and image when archive is valid.
    /// </summary>
    [Fact]
    public async Task RestoreFromZipAsync_ShouldRestoreRecipeAndImage_WhenArchiveIsValid()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string adminUserId = "admin";
        const string recipeId = "restored-recipe";

        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var extra = new Dictionary<string, byte[]>
        {
            ["images/restored-recipe/image01.png"] = imageBytes
        };

        await using var zip = CreateRestoreZip(new ExportRootDto
        {
            ExportedAt = DateTime.UtcNow,
            Users =
            [
                new ExportUserDto
                {
                    Id = "restored-user",
                    UserName = "restored",
                    Email = "restored@source.local",
                    IsAdmin = false
                }
            ],
            Cookbooks =
            [
                new ExportCookbookDto
                {
                    Id = "restored-cookbook",
                    UserId = "restored-user",
                    Title = "Restored cookbook"
                }
            ],
            Recipes =
            [
                new ExportRecipeDto
                {
                    Id = recipeId,
                    OwnerId = "restored-user",
                    Title = "Restored recipe",
                    ImagePaths =
                    [
                        "images/restored-recipe/image01.png"
                    ]
                }
            ]
        }, extra);

        var sut = new ExportService(db, NullLogger<ExportService>.Instance);

        await sut.RestoreFromZipAsync(zip, adminUserId);

        var users = await db.Users.AsNoTracking().ToListAsync();
        users.Should().Contain(u => u.Id == "restored-user");

        var cookbooks = await db.Cookbooks.AsNoTracking().ToListAsync();
        cookbooks.Should().ContainSingle(cb => cb.Id == "restored-cookbook");

        var recipes = await db.Recipes.AsNoTracking().ToListAsync();
        recipes.Should().ContainSingle(r => r.Id == recipeId);

        var images = await db.RecipeImages.AsNoTracking().ToListAsync();
        images.Should().ContainSingle();
        images.Single().RecipeId.Should().Be(recipeId);
        images.Single().Data.Should().Equal(imageBytes);
    }

    private static MemoryStream CreateRestoreZip(ExportRootDto root, Dictionary<string, byte[]>? extraEntries = null, bool includeRecipesJson = true)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (includeRecipesJson)
            {
                var entry = archive.CreateEntry("recipes.json");
                using var entryStream = entry.Open();
                var json = JsonSerializer.Serialize(root, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                var bytes = Encoding.UTF8.GetBytes(json);
                entryStream.Write(bytes, 0, bytes.Length);
            }

            if (extraEntries != null)
            {
                foreach (var (name, data) in extraEntries)
                {
                    var extraEntry = archive.CreateEntry(name);
                    using var extraStream = extraEntry.Open();
                    extraStream.Write(data, 0, data.Length);
                }
            }
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
