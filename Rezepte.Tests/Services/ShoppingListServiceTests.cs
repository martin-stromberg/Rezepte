using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class ShoppingListServiceTests
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

    [Fact]
    public async Task GetGroupsAsync_ShouldCreateDefaultGroup_WhenListIsEmpty()
    {
        using var db = CreateDb();
        var sut = new ShoppingListService(db);

        var groups = await sut.GetGroupsAsync(UserA, CancellationToken.None);

        groups.Should().ContainSingle();
        groups.Single().Name.Should().Be("Einkaufsliste");
        groups.Single().UserId.Should().Be(UserA);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAddItemToUsersGroup()
    {
        using var db = CreateDb();
        var sut = new ShoppingListService(db);
        var group = await sut.EnsureDefaultGroupAsync(UserA, CancellationToken.None);

        var result = await sut.AddItemAsync(UserA, group.Id, 2, "kg", "Kartoffeln", CancellationToken.None);

        result.ok.Should().BeTrue();
        var groups = await sut.GetGroupsAsync(UserA, CancellationToken.None);
        groups.Single().Items.Should().ContainSingle();
        groups.Single().Items.Single().Name.Should().Be("Kartoffeln");
        groups.Single().Items.Single().Amount.Should().Be(2);
        groups.Single().Items.Single().Unit.Should().Be("kg");
    }

    [Fact]
    public async Task SetItemCheckedAsync_ShouldPersistCheckedState()
    {
        using var db = CreateDb();
        var sut = new ShoppingListService(db);
        var group = await sut.EnsureDefaultGroupAsync(UserA, CancellationToken.None);
        var add = await sut.AddItemAsync(UserA, group.Id, 1, null, "Milch", CancellationToken.None);

        var result = await sut.SetItemCheckedAsync(UserA, add.item!.Id, true, CancellationToken.None);

        result.ok.Should().BeTrue();
        var groups = await sut.GetGroupsAsync(UserA, CancellationToken.None);
        groups.Single().Items.Single().IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task AddRecipeIngredientsAsync_ShouldCreateRecipeGroupWithSelectedIngredients()
    {
        using var db = CreateDb();
        var recipe = new Recipe
        {
            UserId = UserA,
            Title = "Pfannkuchen",
            Steps =
            {
                new RecipeStep
                {
                    StepIndex = 0,
                    Description = "Ruehren",
                    Ingredients =
                    {
                        new RecipeIngredient { Amount = 200, Unit = "g", Name = "Mehl" },
                        new RecipeIngredient { Amount = 2, Unit = null, Name = "Eier" }
                    }
                }
            }
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();
        var selectedId = recipe.Steps.Single().Ingredients.First().Id;
        var sut = new ShoppingListService(db);

        var result = await sut.AddRecipeIngredientsAsync(UserA, recipe.Id, new[] { selectedId }, CancellationToken.None);

        result.ok.Should().BeTrue();
        result.group.Should().NotBeNull();
        result.group!.Name.Should().Be("Pfannkuchen");
        result.group.RecipeId.Should().Be(recipe.Id);
        result.group.Items.Should().ContainSingle();
        result.group.Items.Single().Name.Should().Be("Mehl");
    }

    [Fact]
    public async Task AddItemAsync_ShouldRejectGroupFromDifferentUser()
    {
        using var db = CreateDb();
        var sut = new ShoppingListService(db);
        var group = await sut.EnsureDefaultGroupAsync(UserA, CancellationToken.None);

        var result = await sut.AddItemAsync(UserB, group.Id, 1, null, "Falsch", CancellationToken.None);

        result.ok.Should().BeFalse();
        result.error.Should().Be("Gruppe nicht gefunden.");
        (await db.ShoppingListItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetRecipeIngredientGroupsAsync_ShouldReturnMainRecipeAndSideDishes()
    {
        using var db = CreateDb();
        var side = new Recipe
        {
            UserId = UserA,
            Title = "Salat",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Waschen", Ingredients = { new RecipeIngredient { Amount = 1, Name = "Kopf Salat" } } } }
        };
        var main = new Recipe
        {
            UserId = UserA,
            Title = "Lasagne",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Backen", Ingredients = { new RecipeIngredient { Amount = 2, Name = "Tomaten" } } } }
        };
        main.SideDishes.Add(new RecipeSideDish { Recipe = main, SideDishRecipe = side, SideDishRecipeId = side.Id });
        db.Recipes.AddRange(main, side);
        await db.SaveChangesAsync();
        var sut = new ShoppingListService(db);

        var groups = await sut.GetRecipeIngredientGroupsAsync(UserA, main.Id, CancellationToken.None);

        groups.Should().HaveCount(2);
        groups[0].RecipeTitle.Should().Be("Lasagne");
        groups[0].IsMainRecipe.Should().BeTrue();
        groups[1].RecipeTitle.Should().Be("Salat");
        groups[1].IsMainRecipe.Should().BeFalse();
    }

    [Fact]
    public async Task AddRecipeIngredientGroupsAsync_ShouldCreateSeparateGroupsForSelections()
    {
        using var db = CreateDb();
        var side = new Recipe
        {
            UserId = UserA,
            Title = "Salat",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Waschen", Ingredients = { new RecipeIngredient { Amount = 1, Name = "Kopf Salat" } } } }
        };
        var main = new Recipe
        {
            UserId = UserA,
            Title = "Lasagne",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Backen", Ingredients = { new RecipeIngredient { Amount = 2, Name = "Tomaten" } } } }
        };
        main.SideDishes.Add(new RecipeSideDish { Recipe = main, SideDishRecipe = side, SideDishRecipeId = side.Id });
        db.Recipes.AddRange(main, side);
        await db.SaveChangesAsync();
        var mainIngredientId = main.Steps.Single().Ingredients.Single().Id;
        var sideIngredientId = side.Steps.Single().Ingredients.Single().Id;
        var sut = new ShoppingListService(db);

        var result = await sut.AddRecipeIngredientGroupsAsync(
            UserA,
            main.Id,
            new[]
            {
                new ShoppingListRecipeIngredientSelection(main.Id, new[] { mainIngredientId }),
                new ShoppingListRecipeIngredientSelection(side.Id, new[] { sideIngredientId })
            },
            CancellationToken.None);

        result.ok.Should().BeTrue();
        result.groups.Should().HaveCount(2);
        var groups = await sut.GetGroupsAsync(UserA, CancellationToken.None);
        groups.Select(g => g.Name).Should().Contain(new[] { "Lasagne", "Salat" });
    }

    [Fact]
    public async Task AddRecipeIngredientGroupsAsync_ShouldRejectUnlinkedRecipeIngredient()
    {
        using var db = CreateDb();
        var main = new Recipe { UserId = UserA, Title = "Lasagne" };
        var unlinked = new Recipe
        {
            UserId = UserA,
            Title = "Salat",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Waschen", Ingredients = { new RecipeIngredient { Amount = 1, Name = "Kopf Salat" } } } }
        };
        db.Recipes.AddRange(main, unlinked);
        await db.SaveChangesAsync();
        var sut = new ShoppingListService(db);

        var result = await sut.AddRecipeIngredientGroupsAsync(
            UserA,
            main.Id,
            new[] { new ShoppingListRecipeIngredientSelection(unlinked.Id, new[] { unlinked.Steps.Single().Ingredients.Single().Id }) },
            CancellationToken.None);

        result.ok.Should().BeFalse();
        result.error.Should().Be("Mindestens eine ausgewählte Zutat passt nicht zum Rezept.");
    }

    [Fact]
    public async Task AddRecipeIngredientGroupsAsync_ShouldRejectMixedValidAndUnlinkedSelections()
    {
        using var db = CreateDb();
        var main = new Recipe
        {
            UserId = UserA,
            Title = "Lasagne",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Backen", Ingredients = { new RecipeIngredient { Amount = 2, Name = "Tomaten" } } } }
        };
        var unlinked = new Recipe
        {
            UserId = UserA,
            Title = "Salat",
            Steps = { new RecipeStep { StepIndex = 0, Description = "Waschen", Ingredients = { new RecipeIngredient { Amount = 1, Name = "Kopf Salat" } } } }
        };
        db.Recipes.AddRange(main, unlinked);
        await db.SaveChangesAsync();
        var sut = new ShoppingListService(db);

        var result = await sut.AddRecipeIngredientGroupsAsync(
            UserA,
            main.Id,
            new[]
            {
                new ShoppingListRecipeIngredientSelection(main.Id, new[] { main.Steps.Single().Ingredients.Single().Id }),
                new ShoppingListRecipeIngredientSelection(unlinked.Id, new[] { unlinked.Steps.Single().Ingredients.Single().Id })
            },
            CancellationToken.None);

        result.ok.Should().BeFalse();
        result.error.Should().Be("Mindestens eine ausgewählte Zutat passt nicht zum Rezept.");
        (await db.ShoppingListGroups.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddRecipeIngredientGroupsAsync_ShouldMergeDuplicateRecipeSelections()
    {
        using var db = CreateDb();
        var main = new Recipe
        {
            UserId = UserA,
            Title = "Lasagne",
            Steps =
            {
                new RecipeStep
                {
                    StepIndex = 0,
                    Description = "Backen",
                    Ingredients =
                    {
                        new RecipeIngredient { Amount = 2, Name = "Tomaten" },
                        new RecipeIngredient { Amount = 1, Name = "Kaese" }
                    }
                }
            }
        };
        db.Recipes.Add(main);
        await db.SaveChangesAsync();
        var ingredientIds = main.Steps.Single().Ingredients.Select(i => i.Id).ToList();
        var sut = new ShoppingListService(db);

        var result = await sut.AddRecipeIngredientGroupsAsync(
            UserA,
            main.Id,
            new[]
            {
                new ShoppingListRecipeIngredientSelection(main.Id, new[] { ingredientIds[0] }),
                new ShoppingListRecipeIngredientSelection(main.Id, new[] { ingredientIds[1] })
            },
            CancellationToken.None);

        result.ok.Should().BeTrue();
        result.groups.Should().ContainSingle();
        result.groups.Single().Items.Should().HaveCount(2);
    }
}
