using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.BackgroundJobs.Handlers;

/// <summary>
/// Seeds the fixed demo data set (cookbooks, recipes, calendar events and shopping list items)
/// for a newly registered user.
/// </summary>
public class DemoDataSeedingJobHandler : IBackgroundJobHandler
{
    /// <summary>
    /// The job type name under which this handler is registered and enqueued.
    /// </summary>
    public const string JobTypeName = "seed-demo-data";

    /// <summary>
    /// The number of demo calendar events created for the upcoming days.
    /// </summary>
    private const int CalendarEventCount = 5;

    /// <summary>
    /// The fallback portion count used when a demo recipe has no positive portion value.
    /// </summary>
    private const int DefaultPortions = 2;

    /// <summary>
    /// The hour of day at which the demo calendar events are scheduled.
    /// </summary>
    private const int DinnerHour = 18;

    /// <inheritdoc />
    public string JobType => JobTypeName;

    /// <inheritdoc />
    public async Task HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct)
    {
        if (job is null) throw new ArgumentNullException(nameof(job));
        ct.ThrowIfCancellationRequested();

        var payload = DemoDataSeedPayload.FromJson(job.PayloadJson);
        if (string.IsNullOrWhiteSpace(payload.UserId))
        {
            throw new InvalidOperationException("Demo data seeding job has no user id.");
        }

        var userService = scopeServices.GetRequiredService<IUserService>();
        var user = await userService.GetByIdAsync(payload.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{payload.UserId}' does not exist.");
        }

        var cookbookService = scopeServices.GetRequiredService<ICookbookService>();
        var recipeService = scopeServices.GetRequiredService<IRecipeService>();
        var calendarService = scopeServices.GetRequiredService<ICalendarService>();
        var shoppingListService = scopeServices.GetRequiredService<IShoppingListService>();
        var logger = scopeServices.GetRequiredService<ILogger<DemoDataSeedingJobHandler>>();

        logger.LogInformation("Starting demo data seeding for user {UserId}", payload.UserId);

        var cookbookIds = await SeedCookbooksAsync(payload.UserId, cookbookService, ct).ConfigureAwait(false);
        var calendarRecipes = await SeedRecipesAsync(payload.UserId, cookbookIds, recipeService, ct).ConfigureAwait(false);
        var plannedRecipes = await SeedCalendarAsync(payload.UserId, calendarRecipes, calendarService, ct).ConfigureAwait(false);
        await SeedShoppingListAsync(payload.UserId, plannedRecipes, shoppingListService, ct).ConfigureAwait(false);

        logger.LogInformation("Demo data seeding for user {UserId} completed", payload.UserId);
    }

    /// <summary>
    /// Creates all demo cookbooks and returns a map from cookbook name to the created cookbook id.
    /// </summary>
    /// <param name="userId">The id of the user that owns the created cookbooks.</param>
    /// <param name="cookbookService">The cookbook service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A map from cookbook name to cookbook id.</returns>
    private static async Task<Dictionary<string, string>> SeedCookbooksAsync(
        string userId,
        ICookbookService cookbookService,
        CancellationToken ct)
    {
        var cookbookIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cookbook in DemoDataSource.Cookbooks)
        {
            var (ok, error, created) = await cookbookService.CreateAsync(userId, cookbook.Name, null, ct).ConfigureAwait(false);
            if (!ok || created is null)
            {
                throw new InvalidOperationException($"Failed to create cookbook '{cookbook.Name}': {error}");
            }

            cookbookIds[cookbook.Name] = created.Id;
        }

        return cookbookIds;
    }

    /// <summary>
    /// Creates all demo recipes and returns the seeded recipes of the calendar cookbook
    /// (<see cref="DemoDataSource.CalendarCookbookName"/>) as demo/created pairs, so later steps
    /// can use the exact pairing without matching by title.
    /// </summary>
    /// <param name="userId">The id of the user that owns the created recipes.</param>
    /// <param name="cookbookIds">Map from cookbook name to the created cookbook id.</param>
    /// <param name="recipeService">The recipe service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The demo/created recipe pairs of the calendar cookbook.</returns>
    private static async Task<List<SeededRecipe>> SeedRecipesAsync(
        string userId,
        IReadOnlyDictionary<string, string> cookbookIds,
        IRecipeService recipeService,
        CancellationToken ct)
    {
        var calendarRecipes = new List<SeededRecipe>();
        foreach (var cookbook in DemoDataSource.Cookbooks)
        {
            if (!cookbookIds.TryGetValue(cookbook.Name, out var cookbookId))
            {
                throw new InvalidOperationException($"Cookbook '{cookbook.Name}' was not created.");
            }

            foreach (var demoRecipe in cookbook.Recipes)
            {
                var (ok, error, recipe) = await recipeService.CreateAsync(
                    userId,
                    cookbookId,
                    demoRecipe.Title,
                    demoRecipe.Description,
                    null,
                    demoRecipe.Portions,
                    demoRecipe.Steps,
                    ct).ConfigureAwait(false);
                if (!ok || recipe is null)
                {
                    throw new InvalidOperationException($"Failed to create recipe '{demoRecipe.Title}': {error}");
                }

                if (cookbook.Name == DemoDataSource.CalendarCookbookName)
                {
                    calendarRecipes.Add(new SeededRecipe(demoRecipe, recipe));
                }
            }
        }

        return calendarRecipes;
    }

    /// <summary>
    /// Creates one demo calendar event per day for the next <see cref="CalendarEventCount"/> days
    /// using the first recipes of the calendar cookbook.
    /// </summary>
    /// <param name="userId">The id of the user that owns the created events.</param>
    /// <param name="calendarRecipes">The demo/created recipe pairs of the calendar cookbook.</param>
    /// <param name="calendarService">The calendar service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The recipe pairs that were actually planned on the calendar.</returns>
    private static async Task<List<SeededRecipe>> SeedCalendarAsync(
        string userId,
        IReadOnlyList<SeededRecipe> calendarRecipes,
        ICalendarService calendarService,
        CancellationToken ct)
    {
        var plannedRecipes = calendarRecipes.Take(CalendarEventCount).ToList();
        for (var i = 0; i < plannedRecipes.Count; i++)
        {
            var startDate = DateTime.Today.AddDays(i + 1);
            var recipe = plannedRecipes[i].Recipe;
            var (ok, error, _) = await calendarService.CreateEventAsync(
                userId,
                recipe.Id,
                startDate,
                TimeSpan.FromHours(DinnerHour),
                recipe.Portions > 0 ? recipe.Portions : DefaultPortions,
                RecurrenceType.None,
                WeekDays.None,
                ct).ConfigureAwait(false);
            if (!ok)
            {
                throw new InvalidOperationException($"Failed to create calendar event for recipe '{recipe.Title}': {error}");
            }
        }

        return plannedRecipes;
    }

    /// <summary>
    /// Adds all ingredients of the first planned calendar recipe to the user's default shopping list.
    /// </summary>
    /// <param name="userId">The id of the user that owns the shopping list.</param>
    /// <param name="plannedRecipes">The recipe pairs that were planned on the calendar.</param>
    /// <param name="shoppingListService">The shopping list service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task SeedShoppingListAsync(
        string userId,
        IReadOnlyList<SeededRecipe> plannedRecipes,
        IShoppingListService shoppingListService,
        CancellationToken ct)
    {
        var firstPlannedRecipe = plannedRecipes.FirstOrDefault();
        if (firstPlannedRecipe is null)
        {
            return;
        }

        var group = await shoppingListService.EnsureDefaultGroupAsync(userId, ct).ConfigureAwait(false);
        foreach (var step in firstPlannedRecipe.Demo.Steps)
        {
            foreach (var ingredient in step.Ingredients)
            {
                var (ok, error, _) = await shoppingListService.AddItemAsync(
                    userId,
                    group.Id,
                    ingredient.Amount,
                    ingredient.Unit,
                    ingredient.Name,
                    ct).ConfigureAwait(false);
                if (!ok)
                {
                    throw new InvalidOperationException($"Failed to add shopping list item '{ingredient.Name}': {error}");
                }
            }
        }
    }

    /// <summary>
    /// Pairs a demo recipe definition with the recipe entity created from it.
    /// </summary>
    /// <param name="Demo">The demo recipe definition.</param>
    /// <param name="Recipe">The created recipe entity.</param>
    /// <returns>The seeded recipe pair.</returns>
    private sealed record SeededRecipe(DemoRecipe Demo, Recipe Recipe);
}
