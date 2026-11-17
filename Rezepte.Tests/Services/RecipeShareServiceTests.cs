using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Tests.TestSupport;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the recipe share service tests.
/// </summary>
public class RecipeShareServiceTests
{
    private const string OwnerId = "user-owner";
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private static (RecipeShareService Sut, FixedTimeProvider Time) CreateSut(RezepteDbContext db)
    {
        var time = new FixedTimeProvider(Now);
        var sut = new RecipeShareService(db, new SettingsService(db, new SecurityTxtSettingsService(db)), time);
        return (sut, time);
    }

    private static Recipe CreateRecipe(string userId, string title)
    {
        var recipe = new Recipe
        {
            UserId = userId,
            Title = title,
            Description = "Beschreibung",
            Portions = 4,
            CreatedAt = Now.UtcDateTime
        };
        recipe.Steps.Add(new RecipeStep
        {
            StepIndex = 0,
            Title = "Mischen",
            Description = "Alles verrühren",
            DurationMinutes = 10,
            Ingredients = { new RecipeIngredient { Amount = 1, Unit = "Tasse", Name = "Mehl" } }
        });
        recipe.Images.Add(new RecipeImage { FileName = "a.png", ContentType = "image/png", Data = [1, 2, 3] });
        return recipe;
    }

    /// <summary>
    /// Create share link persists link with fresh token and expiry from setting.
    /// </summary>
    [Fact]
    public async Task CreateShareLink_PersistsLink_WithFreshToken_AndExpiryFromSetting()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Kuchen");
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        link.Should().NotBeNull();
        link!.Token.Should().NotBeNullOrEmpty();
        link.Token.Should().MatchRegex("^[A-Za-z0-9_-]{40,50}$");
        link.CreatedAt.Should().Be(Now.UtcDateTime);
        link.ExpiresAt.Should().Be(Now.AddDays(30).UtcDateTime);
        (await db.RecipeShareLinks.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Create share link uses configured validity days.
    /// </summary>
    [Fact]
    public async Task CreateShareLink_UsesConfiguredValidityDays()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Kuchen");
        db.Recipes.Add(recipe);
        db.AppSettings.Add(new AppSetting { Key = "ShareLinkValidityDays", Value = "5" });
        await db.SaveChangesAsync();

        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        link.Should().NotBeNull();
        link!.ExpiresAt.Should().Be(Now.AddDays(5).UtcDateTime);
    }

    /// <summary>
    /// Create share link returns null for foreign or missing recipe.
    /// </summary>
    [Fact]
    public async Task CreateShareLink_ReturnsNull_ForForeignOrMissingRecipe()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe("other-user", "Fremd");
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var foreign = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);
        var missing = await sut.CreateShareLinkAsync(OwnerId, "does-not-exist", includeSideDishes: false, CancellationToken.None);

        foreign.Should().BeNull();
        missing.Should().BeNull();
        (await db.RecipeShareLinks.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Get shared recipe returns view for valid token.
    /// </summary>
    [Fact]
    public async Task GetSharedRecipe_ReturnsView_ForValidToken()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Kuchen");
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        var view = await sut.GetSharedRecipeAsync(link!.Token, CancellationToken.None);

        view.Should().NotBeNull();
        view!.Title.Should().Be("Kuchen");
        view.Portions.Should().Be(4);
        view.Steps.Should().ContainSingle();
        view.Steps[0].Ingredients.Should().ContainSingle(i => i.Name == "Mehl");
        view.ImageUrls.Should().ContainSingle()
            .Which.Should().Be($"/api/share/{link.Token}/image/{recipe.Images.Single().Id}");
    }

    /// <summary>
    /// Get shared recipe returns null for unknown token.
    /// </summary>
    [Fact]
    public async Task GetSharedRecipe_ReturnsNull_ForUnknownToken()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);

        var view = await sut.GetSharedRecipeAsync("unknown-token", CancellationToken.None);

        view.Should().BeNull();
    }

    /// <summary>
    /// Get shared recipe returns null after expiry.
    /// </summary>
    [Fact]
    public async Task GetSharedRecipe_ReturnsNull_AfterExpiry()
    {
        using var db = CreateDb();
        var (sut, time) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Kuchen");
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        time.Advance(TimeSpan.FromDays(31));

        var view = await sut.GetSharedRecipeAsync(link!.Token, CancellationToken.None);
        view.Should().BeNull();
    }

    /// <summary>
    /// Get shared recipe omits side dishes without flag.
    /// </summary>
    [Fact]
    public async Task GetSharedRecipe_OmitsSideDishes_WithoutFlag()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Hauptgericht");
        var sideDish = CreateRecipe(OwnerId, "Beilage");
        db.Recipes.AddRange(recipe, sideDish);
        db.RecipeSideDishes.Add(new RecipeSideDish { RecipeId = recipe.Id, SideDishRecipeId = sideDish.Id });
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        var view = await sut.GetSharedRecipeAsync(link!.Token, CancellationToken.None);

        view.Should().NotBeNull();
        view!.SideDishes.Should().BeEmpty();
    }

    /// <summary>
    /// Get shared recipe includes side dishes with flag.
    /// </summary>
    [Fact]
    public async Task GetSharedRecipe_IncludesSideDishes_WithFlag()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Hauptgericht");
        var sideDish = CreateRecipe(OwnerId, "Beilage");
        db.Recipes.AddRange(recipe, sideDish);
        db.RecipeSideDishes.Add(new RecipeSideDish { RecipeId = recipe.Id, SideDishRecipeId = sideDish.Id });
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: true, CancellationToken.None);

        var view = await sut.GetSharedRecipeAsync(link!.Token, CancellationToken.None);

        view.Should().NotBeNull();
        view!.SideDishes.Should().ContainSingle();
        var entry = view.SideDishes[0];
        entry.RecipeId.Should().Be(sideDish.Id);
        entry.Title.Should().Be("Beilage");
        entry.Url.Should().Be($"/share/{link.Token}/sidedish/{sideDish.Id}");
        entry.ImageUrl.Should().Be($"/api/share/{link.Token}/image/{sideDish.Images.Single().Id}");
    }

    /// <summary>
    /// Get shared side dish returns view only for direct side dish with flag.
    /// </summary>
    [Fact]
    public async Task GetSharedSideDish_ReturnsView_OnlyForDirectSideDish_WithFlag()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Hauptgericht");
        var sideDish = CreateRecipe(OwnerId, "Beilage");
        db.Recipes.AddRange(recipe, sideDish);
        db.RecipeSideDishes.Add(new RecipeSideDish { RecipeId = recipe.Id, SideDishRecipeId = sideDish.Id });
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: true, CancellationToken.None);

        var view = await sut.GetSharedSideDishAsync(link!.Token, sideDish.Id, CancellationToken.None);

        view.Should().NotBeNull();
        view!.Title.Should().Be("Beilage");
        view.SideDishes.Should().BeEmpty();
    }

    /// <summary>
    /// Get shared side dish returns null without flag.
    /// </summary>
    [Fact]
    public async Task GetSharedSideDish_ReturnsNull_WithoutFlag()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Hauptgericht");
        var sideDish = CreateRecipe(OwnerId, "Beilage");
        db.Recipes.AddRange(recipe, sideDish);
        db.RecipeSideDishes.Add(new RecipeSideDish { RecipeId = recipe.Id, SideDishRecipeId = sideDish.Id });
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        var view = await sut.GetSharedSideDishAsync(link!.Token, sideDish.Id, CancellationToken.None);

        view.Should().BeNull();
    }

    /// <summary>
    /// Get shared side dish returns null for non side dish.
    /// </summary>
    [Fact]
    public async Task GetSharedSideDish_ReturnsNull_ForNonSideDish()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Hauptgericht");
        var other = CreateRecipe(OwnerId, "Unverlinkt");
        db.Recipes.AddRange(recipe, other);
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: true, CancellationToken.None);

        var view = await sut.GetSharedSideDishAsync(link!.Token, other.Id, CancellationToken.None);

        view.Should().BeNull();
    }

    /// <summary>
    /// Get shared image allows main recipe image.
    /// </summary>
    [Fact]
    public async Task GetSharedImage_AllowsMainRecipeImage()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Kuchen");
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);

        var image = await sut.GetSharedImageAsync(link!.Token, recipe.Images.Single().Id, CancellationToken.None);

        image.Should().NotBeNull();
        image!.Id.Should().Be(recipe.Images.Single().Id);
    }

    /// <summary>
    /// Get shared image allows side dish image only with flag.
    /// </summary>
    [Fact]
    public async Task GetSharedImage_AllowsSideDishImage_OnlyWithFlag()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Hauptgericht");
        var sideDish = CreateRecipe(OwnerId, "Beilage");
        db.Recipes.AddRange(recipe, sideDish);
        db.RecipeSideDishes.Add(new RecipeSideDish { RecipeId = recipe.Id, SideDishRecipeId = sideDish.Id });
        await db.SaveChangesAsync();
        var flagged = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: true, CancellationToken.None);
        var unflagged = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: false, CancellationToken.None);
        var imageId = sideDish.Images.Single().Id;

        (await sut.GetSharedImageAsync(flagged!.Token, imageId, CancellationToken.None)).Should().NotBeNull();
        (await sut.GetSharedImageAsync(unflagged!.Token, imageId, CancellationToken.None)).Should().BeNull();
    }

    /// <summary>
    /// Get shared image rejects foreign image.
    /// </summary>
    [Fact]
    public async Task GetSharedImage_RejectsForeignImage()
    {
        using var db = CreateDb();
        var (sut, _) = CreateSut(db);
        var recipe = CreateRecipe(OwnerId, "Kuchen");
        var foreign = CreateRecipe(OwnerId, "Fremd");
        db.Recipes.AddRange(recipe, foreign);
        await db.SaveChangesAsync();
        var link = await sut.CreateShareLinkAsync(OwnerId, recipe.Id, includeSideDishes: true, CancellationToken.None);

        var image = await sut.GetSharedImageAsync(link!.Token, foreign.Images.Single().Id, CancellationToken.None);

        image.Should().BeNull();
    }
}
