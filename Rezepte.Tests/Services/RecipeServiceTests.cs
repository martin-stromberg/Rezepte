using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rezepte.Tests.Services;

public class RecipeServiceTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private static IWebHostEnvironment CreateMockEnv()
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.WebRootPath).Returns("wwwroot");
        return mock.Object;
    }
    private static IHttpContextAccessor CreateMockHttpContextAccessor(string userId = UserA)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var mock = new Mock<IHttpContextAccessor>();
        mock.SetupGet(a => a.HttpContext).Returns(context);
        return mock.Object;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreate_WithStepsAndIngredients()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var steps = new[]
        {
            new RecipeCreateStep(
                Title: "Teig",
                Description: "Zutaten mischen",
                DurationMinutes: 10,
                RequiresOvernightRest: false,
                Ingredients: new[] { new RecipeCreateIngredient(200, "g", "Mehl"), new RecipeCreateIngredient(3, "Stk", "Ei") }
            ),
            new RecipeCreateStep(
                Title: null,
                Description: "Backen",
                DurationMinutes: 30,
                RequiresOvernightRest: false,
                Ingredients: Array.Empty<RecipeCreateIngredient>()
            )
        };

        var (ok, error, recipe) = await sut.CreateAsync(UserA, cookbook.Id, "Kuchen", "Leckerer Kuchen", null, portions: null, steps: steps, ct: CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        recipe.Should().NotBeNull();
        recipe!.UserId.Should().Be(UserA);
        var loaded = await sut.GetByIdAsync(UserA, recipe.Id, CancellationToken.None);
        loaded!.Title.Should().Be("Kuchen");
        loaded.Steps.Should().HaveCount(2);
        loaded.Steps.SelectMany(s => s.Ingredients).Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceSteps()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        (bool ok1, string? _, Recipe? recipe) = await sut.CreateAsync(UserA, cookbook.Id, "Salat", null, null, portions: null, steps: new[]
        {
            new RecipeCreateStep(null, "Schneiden", 5, false, Array.Empty<RecipeCreateIngredient>())
        }, CancellationToken.None);
        ok1.Should().BeTrue();

        (bool ok2, string? err2) = await sut.UpdateAsync(UserA, recipe!.Id, "Gemüsesalat", "Frisch", null, null, new[]
        {
            new RecipeCreateStep(null, "Mischen", 3, false, Array.Empty<RecipeCreateIngredient>())
        }, CancellationToken.None);

        ok2.Should().BeTrue();
        err2.Should().BeNull();
        var loaded = await sut.GetByIdAsync(UserA, recipe.Id, CancellationToken.None);
        loaded!.Title.Should().Be("Gemüsesalat");
        loaded.Steps.Should().HaveCount(1);
        loaded.Steps.First().Description.Should().Be("Mischen");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRecipe()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        (bool ok1, string? _, Recipe? recipe) = await sut.CreateAsync(UserA, cookbook.Id, "Suppe", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        ok1.Should().BeTrue();

        (bool ok2, string? err2) = await sut.DeleteAsync(UserA, recipe!.Id, CancellationToken.None);
        ok2.Should().BeTrue();
        err2.Should().BeNull();

        var loaded = await sut.GetByIdAsync(UserA, recipe.Id, CancellationToken.None);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByCookbookAsync_ShouldReturnOnlyRecipesFromGivenCookbook_InTitleOrder()
    {
        using var db = CreateDb();
        var cb1 = new Rezepte.Web.Entities.Cookbook { Name = "A", UserId = UserA };
        var cb2 = new Rezepte.Web.Entities.Cookbook { Name = "B", UserId = UserB };
        db.Cookbooks.AddRange(cb1, cb2);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        await sut.CreateAsync(UserA, cb1.Id, "Z-Titel", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        await sut.CreateAsync(UserA, cb1.Id, "A-Titel", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        await sut.CreateAsync(UserB, cb2.Id, "Andere", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        var list = await sut.GetByCookbookAsync(UserA, cb1.Id, CancellationToken.None);

        list.Should().HaveCount(2);
        list.Select(r => r.Title).Should().ContainInOrder("A-Titel", "Z-Titel");
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenTitleTooShort()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var (ok, error, recipe) = await sut.CreateAsync(UserA, cb.Id, "ab", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailableForCookbookAsync_ShouldReturnRecipes_NotInGivenCookbook()
    {
        using var db = CreateDb();
        var cb1 = new Rezepte.Web.Entities.Cookbook { Name = "CB1", UserId = UserA };
        var cb2 = new Rezepte.Web.Entities.Cookbook { Name = "CB2", UserId = UserB };
        db.Cookbooks.AddRange(cb1, cb2);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        await sut.CreateAsync(UserA, cb1.Id, "R01", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        await sut.CreateAsync(UserB, cb2.Id, "R02", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        await sut.CreateAsync(UserB, cb2.Id, "R03", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        var available = await sut.GetAvailableForCookbookAsync(UserB, cb1.Id, CancellationToken.None);

        available.Select(r => r.Title).Should().BeEquivalentTo(new[] { "R02", "R03" });
    }

    [Fact]
    public async Task SearchAsync_ShouldFindHonigRecipe_ForOwningUser()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Marinaden", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        await sut.CreateAsync(UserA, cookbook.Id, "Honig - Senf - Sojamarinade", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        var result = await sut.SearchAsync(UserA, "Honig", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Select(i => i.Title).Should().Contain("Honig - Senf - Sojamarinade");
    }

    [Fact]
    public async Task SearchAsync_ShouldNotReturnForeignUserRecipes()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Marinaden", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        await sut.CreateAsync(UserA, cookbook.Id, "Honig - Senf - Sojamarinade", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        var result = await sut.SearchAsync(UserB, "Honig", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.Items.Select(i => i.Title).Should().NotContain("Honig - Senf - Sojamarinade");
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByStringCookbookId()
    {
        using var db = CreateDb();
        var matchingCookbook = new Rezepte.Web.Entities.Cookbook { Name = "Marinaden", UserId = UserA };
        var otherCookbook = new Rezepte.Web.Entities.Cookbook { Name = "Desserts", UserId = UserA };
        db.Cookbooks.AddRange(matchingCookbook, otherCookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        await sut.CreateAsync(UserA, matchingCookbook.Id, "Honig - Senf - Sojamarinade", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        await sut.CreateAsync(UserA, otherCookbook.Id, "Honigkuchen", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        var result = await sut.SearchAsync(UserA, "Honig", tags: null, cookbookId: matchingCookbook.Id, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.Items.Select(i => i.Title).Should().ContainSingle()
            .Which.Should().Be("Honig - Senf - Sojamarinade");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task AddExistingToCookbookAsync_ShouldCloneSelected_WithStepsAndIngredients()
    {
        using var db = CreateDb();
        var source = new Rezepte.Web.Entities.Cookbook { Name = "Quelle", UserId = UserB };
        var target = new Rezepte.Web.Entities.Cookbook { Name = "Ziel", UserId = UserB };
        db.Cookbooks.AddRange(source, target);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        (bool ok1, string? error1, Recipe? r1) = await sut.CreateAsync(UserB, source.Id, "R01", "D1", null, portions: null, steps: new[]
        {
            new RecipeCreateStep("S1", "Desc1", 5, false, new[] { new RecipeCreateIngredient(1, "g", "Z1") })
        }, CancellationToken.None);
        ok1.Should().BeTrue($"CreateAsync for r1 failed: {error1}");
        r1.Should().NotBeNull();
        (bool ok2, string? error2, Recipe? r2) = await sut.CreateAsync(UserB, source.Id, "R02", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        ok2.Should().BeTrue($"CreateAsync for r2 failed: {error2}");
        r2.Should().NotBeNull();

        (bool ok, string? err, List<Recipe> created) = await sut.AddExistingToCookbookAsync(UserB, target.Id, new[] { r1!.Id, r2!.Id }, CancellationToken.None);

        ok.Should().BeTrue();
        err.Should().BeNull();
        created.Should().HaveCount(2);
        created.All(c => c.RecipeCookbooks.Any(rc => rc.CookbookId == target.Id)).Should().BeTrue();

        var targetRecipes = await sut.GetByCookbookAsync(UserB, target.Id, CancellationToken.None);
        targetRecipes.Should().HaveCount(2);
        var cloned = await sut.GetByIdAsync(UserB, targetRecipes.First(r => r.Title == "R01").Id, CancellationToken.None);
        cloned!.Steps.Should().HaveCount(1);
        cloned.Steps.First().Ingredients.Should().HaveCount(1);
        cloned.Description.Should().Be("D1");
    }

    [Fact]
    public async Task AddExistingToCookbookAsync_ShouldIgnoreAlreadyInTarget()
    {
        using var db = CreateDb();
        var source = new Rezepte.Web.Entities.Cookbook { Name = "Quelle", UserId = UserB };
        var target = new Rezepte.Web.Entities.Cookbook { Name = "Ziel", UserId = UserB };
        db.Cookbooks.AddRange(source, target);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        (bool _, string? _, Recipe? r1) = await sut.CreateAsync(UserB, target.Id, "SchonDa", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);
        (bool _, string? _, Recipe? r2) = await sut.CreateAsync(UserB, source.Id, "Neu01", null, null, portions: null, steps: Array.Empty<RecipeCreateStep>(), CancellationToken.None);

        (bool ok, string? err, List<Recipe> created) = await sut.AddExistingToCookbookAsync(UserB, target.Id, new[] { r1!.Id, r2!.Id }, CancellationToken.None);

        ok.Should().BeTrue();
        err.Should().BeNull();
        created.Should().HaveCount(1);
        created.Single().Title.Should().Be("Neu01");
    }

    [Fact]
    public async Task AddExistingToCookbookAsync_ShouldReturnEmpty_WhenNoIds()
    {
        using var db = CreateDb();
        var target = new Rezepte.Web.Entities.Cookbook { Name = "Ziel", UserId = UserA };
        db.Cookbooks.Add(target);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        (bool ok, string? err, List<Recipe> created) = await sut.AddExistingToCookbookAsync(UserA, target.Id, Array.Empty<string>(), CancellationToken.None);

        ok.Should().BeTrue();
        err.Should().BeNull();
        created.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldStoreSideDishes_WithoutDuplicates()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        var side = new Recipe { UserId = UserA, Title = "Salat" };
        db.Cookbooks.Add(cookbook);
        db.Recipes.Add(side);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var result = await sut.CreateAsync(UserA, cookbook.Id, "Teller", null, null, null, Array.Empty<RecipeCreateStep>(), new[] { side.Id, side.Id }, CancellationToken.None);

        result.ok.Should().BeTrue();
        var loaded = await sut.GetByIdAsync(UserA, result.recipe!.Id, CancellationToken.None);
        loaded!.SideDishes.Should().ContainSingle();
        loaded.SideDishes.Single().SideDishRecipeId.Should().Be(side.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectForeignSideDish()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        var foreignSide = new Recipe { UserId = UserB, Title = "Fremd" };
        db.Cookbooks.Add(cookbook);
        db.Recipes.Add(foreignSide);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var result = await sut.CreateAsync(UserA, cookbook.Id, "Teller", null, null, null, Array.Empty<RecipeCreateStep>(), new[] { foreignSide.Id }, CancellationToken.None);

        result.ok.Should().BeFalse();
        result.error.Should().Be("Mindestens eine Beilage wurde nicht gefunden.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceSideDishes_AndRejectSelfReference()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var sideA = (await sut.CreateAsync(UserA, cookbook.Id, "Salat", null, null, null, Array.Empty<RecipeCreateStep>(), CancellationToken.None)).recipe!;
        var sideB = (await sut.CreateAsync(UserA, cookbook.Id, "Kartoffeln", null, null, null, Array.Empty<RecipeCreateStep>(), CancellationToken.None)).recipe!;
        var main = (await sut.CreateAsync(UserA, cookbook.Id, "Hauptgericht", null, null, null, Array.Empty<RecipeCreateStep>(), new[] { sideA.Id }, CancellationToken.None)).recipe!;

        var update = await sut.UpdateAsync(UserA, main.Id, "Hauptgericht", null, null, null, Array.Empty<RecipeCreateStep>(), new[] { sideB.Id }, CancellationToken.None);
        var loaded = await sut.GetByIdAsync(UserA, main.Id, CancellationToken.None);
        var self = await sut.UpdateAsync(UserA, main.Id, "Manipuliert", "Soll nicht bleiben", null, null, Array.Empty<RecipeCreateStep>(), new[] { main.Id }, CancellationToken.None);
        var afterFailedUpdate = await sut.GetByIdAsync(UserA, main.Id, CancellationToken.None);

        update.ok.Should().BeTrue();
        loaded!.SideDishes.Should().ContainSingle(sd => sd.SideDishRecipeId == sideB.Id);
        self.ok.Should().BeFalse();
        self.error.Should().Be("Ein Rezept kann nicht seine eigene Beilage sein.");
        afterFailedUpdate!.Title.Should().Be("Hauptgericht");
        afterFailedUpdate.Description.Should().BeNull();
        afterFailedUpdate.SideDishes.Should().ContainSingle(sd => sd.SideDishRecipeId == sideB.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSideDishes_InConfiguredOrder()
    {
        using var db = CreateDb();
        var cookbook = new Rezepte.Web.Entities.Cookbook { Name = "Test", UserId = UserA };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync();

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var sideA = (await sut.CreateAsync(UserA, cookbook.Id, "A-Salat", null, null, null, Array.Empty<RecipeCreateStep>(), CancellationToken.None)).recipe!;
        var sideB = (await sut.CreateAsync(UserA, cookbook.Id, "B-Kartoffeln", null, null, null, Array.Empty<RecipeCreateStep>(), CancellationToken.None)).recipe!;
        var main = (await sut.CreateAsync(UserA, cookbook.Id, "Hauptgericht", null, null, null, Array.Empty<RecipeCreateStep>(), new[] { sideB.Id, sideA.Id }, CancellationToken.None)).recipe!;

        var loaded = await sut.GetByIdAsync(UserA, main.Id, CancellationToken.None);

        loaded!.SideDishes.Select(sd => sd.SideDishRecipeId).Should().ContainInOrder(sideB.Id, sideA.Id);
    }
}
