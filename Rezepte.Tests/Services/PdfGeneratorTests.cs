using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Tests for <see cref="PdfGenerator"/> covering the QuestPDF font configuration:
/// recipes with special characters (emoji) must still produce a PDF instead of
/// silently returning <see langword="null"/>.
/// </summary>
public class PdfGeneratorTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGeneratorTests"/> class and applies
    /// the central QuestPDF configuration (license, font fallback chain).
    /// </summary>
    public PdfGeneratorTests()
    {
        QuestPdfConfiguration.Configure();
    }

    /// <summary>
    /// A recipe whose title and step text contain emoji must produce a non-empty PDF;
    /// the Noto Emoji fallback font renders the glyphs.
    /// </summary>
    [Fact]
    public async Task GenerateRecipePdf_WithEmojiTitle_ReturnsNonEmptyPdf()
    {
        var (connection, db, generator) = await CreateGeneratorAsync();
        await using (connection)
        await using (db)
        {
            var recipe = new ExportRecipeDto
            {
                Id = "recipe-emoji",
                OwnerId = "user1",
                Title = "Pfannkuchen 🥞 mit Äpfeln 🍎",
                Description = "Schnelles Dessert 😋",
                Steps =
                [
                    new ExportStepDto
                    {
                        Id = "step1",
                        StepIndex = 0,
                        Title = "Teig anrühren 🥣",
                        Description = "Mehl, Milch und Eier verrühren.",
                        Ingredients = [new ExportIngredientDto { Id = "ing1", Amount = 250, Unit = "g", Name = "Mehl" }]
                    }
                ]
            };

            var pdf = await generator.GenerateRecipePdfAsync(recipe);

            pdf.Should().NotBeNull();
            pdf!.Length.Should().BeGreaterThan(0);
            pdf.Take(4).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
        }
    }

    /// <summary>
    /// A recipe with glyphs outside the Lato/Noto Emoji fallback chain (CJK) must still
    /// produce a PDF: <c>ThrowOnMissingTextGlyphs = false</c> renders placeholder glyphs
    /// instead of failing the whole document.
    /// </summary>
    [Fact]
    public async Task GenerateRecipePdf_WithCjkGlyphs_ReturnsNonEmptyPdf()
    {
        var (connection, db, generator) = await CreateGeneratorAsync();
        await using (connection)
        await using (db)
        {
            var recipe = new ExportRecipeDto
            {
                Id = "recipe-cjk",
                OwnerId = "user1",
                Title = "中文 Rezept",
                Steps =
                [
                    new ExportStepDto
                    {
                        Id = "step1",
                        StepIndex = 0,
                        Description = "步骤一",
                        Ingredients = []
                    }
                ]
            };

            var pdf = await generator.GenerateRecipePdfAsync(recipe);

            pdf.Should().NotBeNull();
            pdf!.Length.Should().BeGreaterThan(0);
            pdf.Take(4).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
        }
    }

    /// <summary>
    /// The central QuestPDF configuration must be safe to call repeatedly so tests and
    /// the application startup can both apply it without side effects.
    /// </summary>
    [Fact]
    public void Configure_IsIdempotent()
    {
        var act = () =>
        {
            QuestPdfConfiguration.Configure();
            QuestPdfConfiguration.Configure();
        };

        act.Should().NotThrow();
    }

    private static async Task<(SqliteConnection Connection, RezepteDbContext Db, PdfGenerator Generator)> CreateGeneratorAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return (connection, db, new PdfGenerator(db, NullLogger<PdfGenerator>.Instance));
    }
}
