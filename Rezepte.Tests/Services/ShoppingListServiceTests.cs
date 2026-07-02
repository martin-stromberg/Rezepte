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
}
