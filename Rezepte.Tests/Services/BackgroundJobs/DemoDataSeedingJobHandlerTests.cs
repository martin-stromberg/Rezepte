using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Rezepte.Web.Services.BackgroundJobs.Handlers;
using Xunit;

namespace Rezepte.Tests.Services.BackgroundJobs;

/// <summary>
/// Class representing the demo data seeding job handler tests.
/// </summary>
public class DemoDataSeedingJobHandlerTests
{
    /// <summary>
    /// Holds the mocks used by the happy-path tests so each test only wires its own verification.
    /// </summary>
    /// <param name="UserService">The user service mock.</param>
    /// <param name="CookbookService">The cookbook service mock.</param>
    /// <param name="RecipeService">The recipe service mock.</param>
    /// <param name="CalendarService">The calendar service mock.</param>
    /// <param name="ShoppingListService">The shopping list service mock.</param>
    /// <param name="Provider">The service provider containing all mocked services.</param>
    /// <returns>The happy path mocks.</returns>
    private sealed record HappyPathMocks(
        Mock<IUserService> UserService,
        Mock<ICookbookService> CookbookService,
        Mock<IRecipeService> RecipeService,
        Mock<ICalendarService> CalendarService,
        Mock<IShoppingListService> ShoppingListService,
        IServiceProvider Provider);

    private static IServiceProvider BuildProvider(
        Mock<IUserService>? userService = null,
        Mock<ICookbookService>? cookbookService = null,
        Mock<IRecipeService>? recipeService = null,
        Mock<ICalendarService>? calendarService = null,
        Mock<IShoppingListService>? shoppingListService = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(userService?.Object ?? new Mock<IUserService>().Object);
        services.AddSingleton(cookbookService?.Object ?? new Mock<ICookbookService>().Object);
        services.AddSingleton(recipeService?.Object ?? new Mock<IRecipeService>().Object);
        services.AddSingleton(calendarService?.Object ?? new Mock<ICalendarService>().Object);
        services.AddSingleton(shoppingListService?.Object ?? new Mock<IShoppingListService>().Object);
        services.AddSingleton(new Mock<Microsoft.Extensions.Logging.ILogger<DemoDataSeedingJobHandler>>().Object);
        return services.BuildServiceProvider();
    }

    private static BackgroundJob CreateJob(string userId)
    {
        return new BackgroundJob
        {
            PayloadJson = new DemoDataSeedPayload(userId).ToJson()
        };
    }

    /// <summary>
    /// Creates mocks for all services involved in the happy path of the seeding job:
    /// the user exists and every service call succeeds.
    /// </summary>
    /// <param name="userId">The id of the existing user returned by the user service mock.</param>
    /// <returns>The configured mocks together with the service provider.</returns>
    private static HappyPathMocks CreateHappyPathMocks(string userId)
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rezepte.Web.Services.User(userId, "test", string.Empty, string.Empty, false, DateTime.UtcNow));

        var cookbookService = new Mock<ICookbookService>();
        var cookbookId = 0;
        cookbookService.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => cookbookId++)
            .ReturnsAsync(() => (true, null, new Cookbook { Id = cookbookId.ToString(), Name = "Demo" }));

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

        var provider = BuildProvider(userService, cookbookService, recipeService, calendarService, shoppingListService);
        return new HappyPathMocks(userService, cookbookService, recipeService, calendarService, shoppingListService, provider);
    }

    /// <summary>
    /// Handle async should create five cookbooks.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldCreateFiveCookbooks()
    {
        const string userId = "user1";
        var mocks = CreateHappyPathMocks(userId);
        var handler = new DemoDataSeedingJobHandler();

        await handler.HandleAsync(CreateJob(userId), mocks.Provider, CancellationToken.None);

        mocks.CookbookService.Verify(s => s.CreateAsync(userId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    /// <summary>
    /// Handle async should create forty three recipes.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldCreateFortyThreeRecipes()
    {
        const string userId = "user2";
        var mocks = CreateHappyPathMocks(userId);
        var handler = new DemoDataSeedingJobHandler();

        await handler.HandleAsync(CreateJob(userId), mocks.Provider, CancellationToken.None);

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
    /// Handle async should create five calendar events.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldCreateFiveCalendarEvents()
    {
        const string userId = "user3";
        var mocks = CreateHappyPathMocks(userId);
        var handler = new DemoDataSeedingJobHandler();

        await handler.HandleAsync(CreateJob(userId), mocks.Provider, CancellationToken.None);

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
    /// Handle async should add ingredients to default shopping list.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldAddIngredientsToDefaultShoppingList()
    {
        const string userId = "user4";
        var mocks = CreateHappyPathMocks(userId);
        var handler = new DemoDataSeedingJobHandler();

        await handler.HandleAsync(CreateJob(userId), mocks.Provider, CancellationToken.None);

        mocks.ShoppingListService.Verify(s => s.AddItemAsync(
            userId,
            "1",
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    /// <summary>
    /// Handle async should throw when user does not exist.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        const string userId = "user5";
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((Rezepte.Web.Services.User?)null);

        var provider = BuildProvider(userService);
        var handler = new DemoDataSeedingJobHandler();

        var action = () => handler.HandleAsync(CreateJob(userId), provider, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Handle async should throw when any service operation fails.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenAnyServiceOperationFails()
    {
        const string userId = "user6";
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new Rezepte.Web.Services.User(userId, "test", string.Empty, string.Empty, false, DateTime.UtcNow));

        var cookbookService = new Mock<ICookbookService>();
        var callCount = 0;
        cookbookService.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(() => callCount == 2
                ? (false, "Cookbook creation failed.", (Cookbook?)null)
                : (true, null, new Cookbook { Id = callCount.ToString(), Name = "Demo" }));

        var provider = BuildProvider(userService, cookbookService);
        var handler = new DemoDataSeedingJobHandler();

        var action = () => handler.HandleAsync(CreateJob(userId), provider, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
