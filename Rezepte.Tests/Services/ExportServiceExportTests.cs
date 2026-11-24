using System.IO.Compression;
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
/// Class representing the export service export tests.
/// </summary>
public class ExportServiceExportTests
{
    /// <summary>
    /// Export user async should return valid zip with image.
    /// </summary>
    [Fact]
    public async Task ExportUserAsync_ShouldReturnValidZipWithImage()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string userId = "user1";
        const string recipeId = "recipe1";

        db.Cookbooks.Add(new Cookbook
        {
            Id = "cookbook1",
            UserId = userId,
            Name = "Kochbuch"
        });

        db.Recipes.Add(new Recipe
        {
            Id = recipeId,
            UserId = userId,
            Title = "Rezept",
            CreatedAt = DateTime.UtcNow,
            Images =
            [
                new RecipeImage
                {
                    Id = "image1",
                    RecipeId = recipeId,
                    FileName = "photo.jpg",
                    ContentType = "image/jpeg",
                    Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 },
                    CreatedAt = DateTime.UtcNow
                }
            ]
        });

        await db.SaveChangesAsync();

        var sut = new ExportService(db, NullLogger<ExportService>.Instance);

        await using var zip = await sut.ExportUserAsync(userId, includeImages: true, includePdf: false);

        zip.Should().NotBeNull();
        zip.CanSeek.Should().BeTrue();
        zip.Length.Should().BeGreaterThan(0);

        zip.Seek(0, SeekOrigin.Begin);
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);

        archive.Entries.Should().Contain(e => e.FullName == "recipes.json");
        archive.Entries.Should().Contain(e => e.FullName == $"images/{recipeId}/image01.jpg");
    }

    /// <summary>
    /// Export user async with includePdf should place a generated PDF per recipe into
    /// the zip, even when the recipe title contains emoji characters.
    /// </summary>
    [Fact]
    public async Task ExportUserAsync_WithPdfIncluded_ContainsPdfEntriesInZip()
    {
        QuestPdfConfiguration.Configure();

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        const string userId = "user1";
        const string recipeId = "recipe1";

        db.Recipes.Add(new Recipe
        {
            Id = recipeId,
            UserId = userId,
            Title = "Pfannkuchen 🥞",
            CreatedAt = DateTime.UtcNow,
            Steps =
            [
                new RecipeStep
                {
                    Id = "step1",
                    RecipeId = recipeId,
                    StepIndex = 0,
                    Title = "Teig",
                    Description = "Mehl, Milch und Eier verrühren.",
                    Ingredients = [new RecipeIngredient { Id = "ing1", StepId = "step1", Amount = 250, Unit = "g", Name = "Mehl" }]
                }
            ]
        });

        await db.SaveChangesAsync();

        var pdfGenerator = new PdfGenerator(db, NullLogger<PdfGenerator>.Instance);
        var sut = new ExportService(db, NullLogger<ExportService>.Instance, pdfGenerator);

        await using var zip = await sut.ExportUserAsync(userId, includeImages: false, includePdf: true);

        zip.Seek(0, SeekOrigin.Begin);
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);

        var pdfEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("pdf/", StringComparison.Ordinal) && e.FullName.EndsWith(".pdf", StringComparison.Ordinal))
            .ToList();

        pdfEntries.Should().HaveCount(1);
        pdfEntries[0].Length.Should().BeGreaterThan(0);
    }
}
