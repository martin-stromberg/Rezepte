using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rezepte.Web.Data;
using Rezepte.Web.Services;

namespace Rezepte.Web.Services;

/// <summary>
/// Simple PDF generator using QuestPDF.
/// Produces a PDF with title, optional first image and the ordered preparation steps.
/// </summary>
public class PdfGenerator : IPdfGenerator
{
    private readonly RezepteDbContext _db;
    private readonly ILogger<PdfGenerator> _logger;

    public PdfGenerator(RezepteDbContext db, ILogger<PdfGenerator> logger)
    {
        _db = db;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]?> GenerateRecipePdfAsync(ExportRecipeDto recipe, CancellationToken ct = default)
    {
        if (recipe is null) throw new ArgumentNullException(nameof(recipe));

        try
        {
            // Load first image binary (if any) for the recipe
            byte[]? imageBytes = null;
            var imgEntity = await _db.RecipeImages
                .AsNoTracking()
                .Where(i => i.RecipeId == recipe.Id)
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (imgEntity != null && imgEntity.Data != null && imgEntity.Data.Length > 0)
            {
                imageBytes = imgEntity.Data;
            }

            // Create document
            var doc = new RecipePdfDocument(recipe, imageBytes);
            await using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PDF generation cancelled for recipe {RecipeId}", recipe.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for recipe {RecipeId}", recipe.Id);
            return null;
        }
    }

    private class RecipePdfDocument : IDocument
    {
        private readonly ExportRecipeDto _recipe;
        private readonly byte[]? _imageBytes;

        public RecipePdfDocument(ExportRecipeDto recipe, byte[]? imageBytes)
        {
            _recipe = recipe;
            _imageBytes = imageBytes;
        }

        public DocumentMetadata GetMetadata()
        {
            return DocumentMetadata.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Column(column =>
                {
                    column.Item().Text(x =>
                    {
                        x.Span("Exported: ").SemiBold();
                        x.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                    });
                    // Add more items if needed
                });
            });
        }

        void ComposeHeader(IContainer header)
        {
            header.PaddingBottom(10).Row(row =>
            {
                row.RelativeColumn().Stack(stack =>
                {
                    stack.Item().Text(_recipe.Title ?? "Rezept").FontSize(18).Bold();
                    if (!string.IsNullOrWhiteSpace(_recipe.OwnerId))
                    {
                        stack.Item().Text($"Autor: {_recipe.OwnerId}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    }
                    if (!string.IsNullOrWhiteSpace(_recipe.Description))
                    {
                        stack.Item().PaddingTop(6).Text(_recipe.Description).FontSize(11).FontColor(Colors.Grey.Darken1);
                    }
                });

                if (_imageBytes != null)
                {
                    row.ConstantColumn(120).Height(90).AlignRight().Element(img =>
                    {
                        using var imgStream = new MemoryStream(_imageBytes);
                        img.Image(imgStream, ImageScaling.FitArea);
                    });
                }
            });
        }

        void ComposeContent(IContainer content)
        {
            content.PaddingTop(5).Column(column =>
            {
                column.Spacing(8);

                if (_recipe.Steps != null && _recipe.Steps.Count > 0)
                {
                    foreach (var step in _recipe.Steps.OrderBy(s => s.StepIndex))
                    {
                        column.Item().Element(c =>
                        {
                            c.Padding(6).Border(1).BorderColor(Colors.Grey.Lighten3).Column(stack =>
                            {
                                stack.Spacing(4);
                                stack.Item().Row(r =>
                                {
                                    r.ConstantItem(60).Text($"Schritt {step.StepIndex + 1}").Bold();
                                    r.RelativeItem().Text(step.Title ?? "(ohne Titel)").SemiBold();
                                });
                                stack.Item().Text(step.Description).FontSize(11);
                                if (step.Ingredients != null && step.Ingredients.Count > 0)
                                {
                                    stack.Item().PaddingTop(6).Text("Zutaten:").Bold();
                                    stack.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(50);
                                            columns.ConstantColumn(50);
                                            columns.RelativeColumn();
                                        });

                                        // Tabellenkopf (optional)
                                        table.Header(header =>
                                        {
                                            header.Cell().Text("Menge").FontSize(10).SemiBold();
                                            header.Cell().Text("Einheit").FontSize(10).SemiBold();
                                            header.Cell().Text("Name").FontSize(10).SemiBold();
                                        });

                                        foreach (var ing in step.Ingredients)
                                        {
                                            table.Cell().Text(ing.Amount.ToString()).FontSize(10);
                                            table.Cell().Text(ing.Unit ?? string.Empty).FontSize(10);
                                            table.Cell().Text(ing.Name).FontSize(10);
                                        }
                                    });
                                }
                            });
                        });
                    }
                }
                else
                {
                    column.Item().Text("Keine Zubereitungsschritte vorhanden.").Italic();
                }
            });
        }
    }
}