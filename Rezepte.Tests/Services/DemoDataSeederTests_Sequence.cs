using Moq;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Tests covering the happy-path call sequence of <see cref="DemoDataSeeder.SeedForUserAsync"/>.
/// </summary>
public class DemoDataSeederTests_Sequence
{
    /// <summary>
    /// Seed for user async should create five cookbooks.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WithDemoDataSource_CreatesFiveCookbooks()
    {
        const string userId = "user1";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        await mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        mocks.CookbookService.Verify(s => s.CreateAsync(userId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    /// <summary>
    /// Seed for user async should create forty three recipes.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WithDemoDataSource_CreatesFortyThreeRecipes()
    {
        const string userId = "user2";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        await mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        mocks.RecipeService.Verify(s => s.CreateAsync(
            userId,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            null,
            It.IsAny<int?>(),
            It.IsAny<IReadOnlyList<RecipeCreateStep>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(43));
    }

    /// <summary>
    /// Seed for user async should create five calendar events.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WithDemoDataSource_CreatesFiveCalendarEvents()
    {
        const string userId = "user3";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        await mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        for (var i = 1; i <= 5; i++)
        {
            var day = DateTime.Today.AddDays(i);
            mocks.CalendarService.Verify(s => s.CreateEventAsync(
                userId,
                It.IsAny<string>(),
                day,
                TimeSpan.FromHours(18),
                It.IsAny<int>(),
                RecurrenceType.None,
                WeekDays.None,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    /// <summary>
    /// Seed for user async should add ingredients to default shopping list.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WithDemoDataSource_AddsIngredientsToDefaultShoppingList()
    {
        const string userId = "user4";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        await mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        mocks.ShoppingListService.Verify(s => s.EnsureDefaultGroupAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        mocks.ShoppingListService.Verify(s => s.AddItemAsync(
            userId,
            "1",
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(6));
    }
}
