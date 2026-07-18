using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Rezepte.Import.Plugins.Backup;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public class ProductiveImportPluginParserTests
{
    [Fact]
    public async Task BackupPlugin_ShouldParseRecipesJsonFromZip()
    {
        await using var stream = CreateBackupZip();
        var handler = new BackupImportHandler();

        (await handler.CanHandleAsync(stream, "backup.zip")).Should().BeTrue();
        stream.Position = 0;
        var result = await handler.HandleAsync(stream, "backup.zip", null, "cookbook-1", "user-1");

        result.Success.Should().BeTrue();
        var recipe = result.ImportedRecipes.Should().ContainSingle().Subject;
        recipe.Title.Should().Be("Backup-Rezept");
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Mehl" && i.Quantity == "200 g");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Alles verruehren.");
        recipe.Images.Should().ContainSingle(i => i.FileName == "image.png" && i.ContentType == "image/png");
    }

    private static MemoryStream CreateBackupZip()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var json = """
                {
                  "recipes": [
                    {
                      "title": "Backup-Rezept",
                      "description": "Backup Beschreibung",
                      "uri": "https://example.test/backup",
                      "portions": 2,
                      "steps": [
                        {
                          "description": "Alles verruehren.",
                          "ingredients": [
                            { "amount": 200, "unit": "g", "name": "Mehl" }
                          ]
                        }
                      ],
                      "imagePaths": [ "images/image.png" ]
                    }
                  ]
                }
                """;
            var jsonEntry = archive.CreateEntry("recipes.json");
            using (var writer = new StreamWriter(jsonEntry.Open(), Encoding.UTF8))
                writer.Write(json);

            var imageEntry = archive.CreateEntry("images/image.png");
            using var image = imageEntry.Open();
            image.Write([0x89, 0x50, 0x4E, 0x47, 0x00]);
        }

        stream.Position = 0;
        return stream;
    }
}
