using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Rezepte.Import.Abstractions;
using Rezepte.Import.Plugins.Backup;
using Rezepte.Import.Plugins.Chefkoch;
using Rezepte.Import.Plugins.FifthSource;
using Rezepte.Import.Plugins.FourthSource;
using Rezepte.Import.Plugins.SecondSource;
using Rezepte.Import.Plugins.SixthSource;
using Rezepte.Import.Plugins.ThirdSource;
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

    [Fact]
    public async Task ChefkochPlugin_ShouldParseChefkochHtml()
    {
        var html = """
            <html>
              <head><meta property="og:url" content="https://example.test/chefkoch"></head>
              <body>
                <main>
                  <h1>Chefkoch-Rezept</h1>
                  <section class="recipe-ingredients">
                    <input value="2" />
                    <table><tr><td>200 g</td><td><span>Mehl</span></td></tr></table>
                  </section>
                  <section>
                    <h2>Zubereitung</h2>
                    <div class="instruction-row"><span class="instruction__text">Teig ruehren.</span></div>
                  </section>
                  <div class="recipe-meta-property-group__labels">
                    <div class="recipe-meta-property-group__value">1 Std. 15 Min.</div>
                    <div class="recipe-meta-property-group__title">Arbeitszeit</div>
                  </div>
                </main>
              </body>
            </html>
            """;

        var recipe = await ParseSingleRecipeAsync(new ChefkochImportHandler(), "chefkoch.html", html);

        recipe.Title.Should().Be("Chefkoch-Rezept");
        recipe.SourceUri.Should().Be("https://example.test/chefkoch");
        recipe.WorkTimeMinutes.Should().Be(75);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Mehl" && i.Quantity == "200 g");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Teig ruehren.");
    }

    [Fact]
    public async Task ChefkochPlugin_ShouldReadCollectionPreviewWithoutImportingRecipes()
    {
        var html = """
            <html>
              <head><title>Erdbeerzeit</title></head>
              <body>
                <main>
                  <h1>Erdbeerzeit</h1>
                  <article>
                    <a href="/rezepte/1234567890/Erdbeerkuchen.html">
                      <img src="/img/erdbeerkuchen.jpg" />
                      Erdbeerkuchen
                    </a>
                  </article>
                  <article>
                    <a href="https://www.chefkoch.de/rezepte/9876543210/Erdbeer-Dessert.html">Erdbeer Dessert</a>
                  </article>
                </main>
              </body>
            </html>
            """;

        var handler = new ChefkochImportHandler();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        var preview = await handler.TryReadCollectionPreviewAsync(stream, "Erdbeerzeit.html", "https://www.chefkoch.de/rezeptsammlung/3212418/Erdbeerzeit.html");

        preview.Should().NotBeNull();
        preview!.Title.Should().Be("Erdbeerzeit");
        preview.Items.Should().HaveCount(2);
        preview.Items.Should().Contain(i => i.Id == "chefkoch-1234567890" && i.Title == "Erdbeerkuchen");
        preview.Items.Should().Contain(i => i.Id == "chefkoch-9876543210" && i.Url == "https://www.chefkoch.de/rezepte/9876543210/Erdbeer-Dessert.html");
    }

    [Fact]
    public async Task ChefkochPlugin_ShouldDeduplicateCollectionPreviewUrlVariantsByRecipeId()
    {
        var html = """
            <html>
              <body>
                <main>
                  <h1>Erdbeerzeit</h1>
                  <a href="/rezepte/1234567890/Erdbeerkuchen.html?utm_source=list">Erdbeerkuchen</a>
                  <a href="https://www.chefkoch.de/rezepte/1234567890/Erdbeerkuchen.html">Erdbeerkuchen nochmal</a>
                  <a href="/rezepte/9876543210/Erdbeer-Dessert.html?portionen=4">Erdbeer Dessert</a>
                </main>
              </body>
            </html>
            """;

        var handler = new ChefkochImportHandler();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        var preview = await handler.TryReadCollectionPreviewAsync(stream, "Erdbeerzeit.html", "https://www.chefkoch.de/rezeptsammlung/3212418/Erdbeerzeit.html");

        preview.Should().NotBeNull();
        preview!.Items.Should().HaveCount(2);
        preview.Items.Select(i => i.Id).Should().OnlyHaveUniqueItems();
        preview.Items.Should().ContainSingle(i => i.Id == "chefkoch-1234567890");
        preview.Items.Should().ContainSingle(i => i.Id == "chefkoch-9876543210");
        preview.Items.Should().Contain(i => i.Url == "https://www.chefkoch.de/rezepte/1234567890/Erdbeerkuchen.html");
        preview.Items.Should().Contain(i => i.Url == "https://www.chefkoch.de/rezepte/9876543210/Erdbeer-Dessert.html");
    }

    [Fact]
    public async Task ChefkochPlugin_ShouldNotTreatSingleRecipeAsCollection()
    {
        var html = """
            <html>
              <body>
                <main>
                  <h1>Chefkoch-Rezept</h1>
                  <a href="/rezepte/1234567890/Erdbeerkuchen.html">Erdbeerkuchen</a>
                </main>
              </body>
            </html>
            """;

        var handler = new ChefkochImportHandler();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        var preview = await handler.TryReadCollectionPreviewAsync(stream, "recipe.html", "https://www.chefkoch.de/rezepte/1234567890/Erdbeerkuchen.html");

        preview.Should().BeNull();
    }

    [Fact]
    public async Task SecondSourcePlugin_ShouldParseJsonLdRecipe()
    {
        var html = Script(new
        {
            name = "Second-Rezept",
            description = "Second Beschreibung",
            mainEntityOfPage = new { id = "https://example.test/second" },
            recipeYield = "4",
            prepTime = "PT10M",
            cookTime = "PT20M",
            recipeIngredient = new[] { "250 g Kartoffeln" },
            recipeInstructions = new[] { new { text = "Kartoffeln kochen." } }
        }).Replace("\"id\"", "\"@id\"");

        var recipe = await ParseSingleRecipeAsync(new SecondSourceImportHandler(), "second.html", html);

        recipe.Title.Should().Be("Second-Rezept");
        recipe.Description.Should().Be("Second Beschreibung");
        recipe.SourceUri.Should().Be("https://example.test/second");
        recipe.Portions.Should().Be(4);
        recipe.WorkTimeMinutes.Should().Be(30);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Kartoffeln" && i.Quantity == "250 g");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Kartoffeln kochen.");
    }

    [Fact]
    public async Task ThirdSourcePlugin_ShouldParseStructuredRecipeObject()
    {
        var html = Script(new
        {
            type = "Recipe",
            name = "Third-Rezept",
            description = "Third Beschreibung",
            recipeYield = "3",
            prepTime = "PT5M",
            cookTime = "PT25M",
            recipeIngredient = new[] { "1 EL Oel" },
            recipeInstructions = new[] { new { text = "Anbraten." } }
        }).Replace("\"type\"", "\"@type\"");

        var recipe = await ParseSingleRecipeAsync(new ThirdSourceImportHandler(), "third.html", html);

        recipe.Title.Should().Be("Third-Rezept");
        recipe.Description.Should().Be("Third Beschreibung");
        recipe.Portions.Should().Be(3);
        recipe.WorkTimeMinutes.Should().Be(30);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Oel" && i.Quantity == "1 EL");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Anbraten.");
    }

    [Fact]
    public async Task FourthSourcePlugin_ShouldParseNextPageRecipeData()
    {
        var html = Script(new
        {
            props = new
            {
                pageProps = new
                {
                    content = new
                    {
                        body = new object[]
                        {
                            new { component = "paragraph", text = "Ruehren." }
                        }
                    },
                    recipeStructuredData = new
                    {
                        name = "Fourth-Rezept",
                        description = "Fourth Beschreibung",
                        canonicalUrl = "https://example.test/fourth",
                        prepTime = "PT12M",
                        cookTime = "PT18M",
                        imageUrl = (string?)null,
                        recipeIngredient = new[] { "500 ml Wasser" }
                    }
                }
            }
        });

        var recipe = await ParseSingleRecipeAsync(new FourthSourceImportHandler(), "fourth.html", html);

        recipe.Title.Should().Be("Fourth-Rezept");
        recipe.Description.Should().Be("Fourth Beschreibung");
        recipe.SourceUri.Should().Be("https://example.test/fourth");
        recipe.WorkTimeMinutes.Should().Be(30);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Wasser" && i.Quantity == "500 ml");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Ruehren.");
    }

    [Fact]
    public async Task FifthSourcePlugin_ShouldParseGraphRecipe()
    {
        var html = Script(new
        {
            graph = new object[]
            {
                new
                {
                    type = "Recipe",
                    name = "Fifth-Rezept",
                    description = "Fifth Beschreibung",
                    mainEntityOfPage = "https://example.test/fifth",
                    recipeYield = new[] { "5 Portionen" },
                    prepTime = "PT8M",
                    cookTime = "PT22M",
                    recipeIngredient = new[] { "2 Stk Eier" },
                    recipeInstructions = new[] { new { text = "Eier schlagen." } }
                }
            }
        }).Replace("\"graph\"", "\"@graph\"").Replace("\"type\"", "\"@type\"");

        var recipe = await ParseSingleRecipeAsync(new FifthSourceImportHandler(), "fifth.html", html);

        recipe.Title.Should().Be("Fifth-Rezept");
        recipe.Description.Should().Be("Fifth Beschreibung");
        recipe.SourceUri.Should().Be("https://example.test/fifth");
        recipe.Portions.Should().Be(5);
        recipe.WorkTimeMinutes.Should().Be(30);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Eier" && i.Quantity == "2 Stk");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Eier schlagen.");
    }

    [Fact]
    public async Task SixthSourcePlugin_ShouldParseGraphRecipe()
    {
        var html = Script(new
        {
            graph = new object[]
            {
                new
                {
                    type = "Recipe",
                    name = "Sixth-Rezept",
                    description = "Sixth Beschreibung",
                    author = new { url = "https://example.test/sixth" },
                    recipeYield = "6 Portionen",
                    prepTime = "PT11M",
                    cookTime = "PT19M",
                    recipeIngredient = new[] { "3 TL Salz" },
                    recipeInstructions = new[] { "Wuerzen." }
                }
            }
        }).Replace("\"graph\"", "\"@graph\"").Replace("\"type\"", "\"@type\"");

        var recipe = await ParseSingleRecipeAsync(new SixthSourceImportHandler(), "sixth.html", html);

        recipe.Title.Should().Be("Sixth-Rezept");
        recipe.Description.Should().Be("Sixth Beschreibung");
        recipe.SourceUri.Should().Be("https://example.test/sixth");
        recipe.Portions.Should().Be(6);
        recipe.WorkTimeMinutes.Should().Be(30);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Salz" && i.Quantity == "3 TL");
        recipe.Steps.Should().ContainSingle(s => s.Text == "Wuerzen.");
    }

    private static async Task<ImportedRecipe> ParseSingleRecipeAsync(IImportHandler handler, string fileName, string content)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        (await handler.CanHandleAsync(stream, fileName)).Should().BeTrue();
        var result = await handler.HandleAsync(stream, fileName, null, "cookbook-1", "user-1");
        result.Success.Should().BeTrue();
        return result.ImportedRecipes.Should().ContainSingle().Subject;
    }

    private static string Script(object payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return $"""<html><body><script type="application/ld+json">{json}</script></body></html>""";
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
