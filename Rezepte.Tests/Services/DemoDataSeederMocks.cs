using Microsoft.Extensions.Logging;
using Moq;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;

namespace Rezepte.Tests.Services;

/// <summary>
/// Provides mock factories shared by the <see cref="DemoDataSeeder"/> test classes.
/// </summary>
internal static class DemoDataSeederMocks
{
    /// <summary>
    /// Holds the mocks used by the demo data seeder tests so each test only wires its own verification.
    /// </summary>
    /// <param name="CookbookService">The cookbook service mock.</param>
    /// <param name="RecipeService">The recipe service mock.</param>
    /// <param name="CalendarService">The calendar service mock.</param>
    /// <param name="ShoppingListService">The shopping list service mock.</param>
    /// <param name="Logger">The logger mock.</param>
    /// <param name="Seeder">The seeder under test built from the mocks.</param>
    /// <returns>The happy path mocks.</returns>
    internal sealed record Mocks(
        Mock<ICookbookService> CookbookService,
        Mock<IRecipeService> RecipeService,
        Mock<ICalendarService> CalendarService,
        Mock<IShoppingListService> ShoppingListService,
        Mock<ILogger<DemoDataSeeder>> Logger,
        DemoDataSeeder Seeder);

    /// <summary>
    /// Creates mocks for all services involved in the happy path of the demo data seeding:
    /// every service call succeeds.
    /// </summary>
    /// <returns>The configured mocks together with the seeder under test.</returns>
    internal static Mocks CreateHappyPath()
    {
        var cookbookService = CreateCookbookServiceMock();
        var recipeService = CreateRecipeServiceMock();
        var calendarService = CreateCalendarServiceMock();
        var shoppingListService = CreateShoppingListServiceMock();
        var logger = new Mock<ILogger<DemoDataSeeder>>();

        var seeder = new DemoDataSeeder(
            cookbookService.Object,
            recipeService.Object,
            calendarService.Object,
            shoppingListService.Object,
            logger.Object);

        return new Mocks(cookbookService, recipeService, calendarService, shoppingListService, logger, seeder);
    }

    /// <summary>
    /// Creates a cookbook service mock that succeeds and returns incrementing cookbook ids.
    /// </summary>
    /// <returns>The cookbook service mock.</returns>
    internal static Mock<ICookbookService> CreateCookbookServiceMock()
    {
        var cookbookService = new Mock<ICookbookService>();
        var cookbookId = 0;
        cookbookService.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => cookbookId++)
            .ReturnsAsync(() => (true, null, new Cookbook { Id = cookbookId.ToString(), Name = "Demo" }));
        return cookbookService;
    }

    /// <summary>
    /// Creates a recipe service mock that succeeds and returns a recipe carrying the requested title.
    /// </summary>
    /// <returns>The recipe service mock.</returns>
    internal static Mock<IRecipeService> CreateRecipeServiceMock()
    {
        var recipeService = new Mock<IRecipeService>();
        var lastTitle = string.Empty;
        recipeService.Setup(s => s.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyList<RecipeCreateStep>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string?, string, string?, string?, int?, IReadOnlyList<RecipeCreateStep>, CancellationToken>((u, c, t, d, r, p, st, ct) => lastTitle = t)
            .ReturnsAsync(() => (true, null, new Recipe { Id = "1", Title = lastTitle, Portions = 4 }));
        return recipeService;
    }

    /// <summary>
    /// Creates a calendar service mock that succeeds.
    /// </summary>
    /// <returns>The calendar service mock.</returns>
    internal static Mock<ICalendarService> CreateCalendarServiceMock()
    {
        var calendarService = new Mock<ICalendarService>();
        calendarService.Setup(s => s.CreateEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<int>(),
                It.IsAny<RecurrenceType>(),
                It.IsAny<WeekDays>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, new CalendarEvent()));
        return calendarService;
    }

    /// <summary>
    /// Creates a shopping list service mock that succeeds and returns a default group with id "1".
    /// </summary>
    /// <returns>The shopping list service mock.</returns>
    internal static Mock<IShoppingListService> CreateShoppingListServiceMock()
    {
        var shoppingListService = new Mock<IShoppingListService>();
        shoppingListService.Setup(s => s.EnsureDefaultGroupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShoppingListGroup { Id = "1", Name = "Einkaufsliste" });
        shoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, new ShoppingListItem()));
        return shoppingListService;
    }
}
