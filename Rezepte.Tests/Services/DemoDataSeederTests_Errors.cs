using FluentAssertions;
using Moq;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Tests covering the error and tolerance behavior of <see cref="DemoDataSeeder.SeedForUserAsync"/>.
/// </summary>
public class DemoDataSeederTests_Errors
{
    /// <summary>
    /// Seed for user async should throw when cookbook creation fails.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WhenCookbookCreationFails_ThrowsInvalidOperationException()
    {
        const string userId = "user5";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        var callCount = 0;
        mocks.CookbookService.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(() => callCount == 2
                ? (false, "Cookbook creation failed.", (Cookbook?)null)
                : (true, null, new Cookbook { Id = callCount.ToString(), Name = "Demo" }));

        var action = () => mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Seed for user async should throw when recipe creation fails.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WhenRecipeCreationFails_ThrowsInvalidOperationException()
    {
        const string userId = "user6";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        mocks.RecipeService.Setup(s => s.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyList<RecipeCreateStep>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Recipe creation failed.", (Recipe?)null));

        var action = () => mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Seed for user async should throw when calendar event creation fails.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WhenCalendarEventCreationFails_ThrowsInvalidOperationException()
    {
        const string userId = "user7";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        mocks.CalendarService.Setup(s => s.CreateEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<int>(),
                It.IsAny<RecurrenceType>(),
                It.IsAny<WeekDays>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Calendar event creation failed.", (CalendarEvent?)null));

        var action = () => mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Seed for user async should throw when a shopping list item cannot be added.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WhenShoppingListItemFails_ThrowsInvalidOperationException()
    {
        const string userId = "user8";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        mocks.ShoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Shopping list item failed.", (ShoppingListItem?)null));

        var action = () => mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Seed for user async should continue seeding when attaching a demo image fails.
    /// </summary>
    [Fact]
    public async Task SeedForUserAsync_WhenImageAttachFails_ContinuesSeeding()
    {
        const string userId = "user9";
        var mocks = DemoDataSeederMocks.CreateHappyPath();

        mocks.RecipeService.Setup(s => s.AddImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Image attach failed.", (RecipeImage?)null));

        await mocks.Seeder.SeedForUserAsync(userId, CancellationToken.None);

        mocks.CalendarService.Verify(s => s.CreateEventAsync(
            userId,
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            TimeSpan.FromHours(18),
            It.IsAny<int>(),
            RecurrenceType.None,
            WeekDays.None,
            It.IsAny<CancellationToken>()), Times.Exactly(5));
        mocks.ShoppingListService.Verify(s => s.EnsureDefaultGroupAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
