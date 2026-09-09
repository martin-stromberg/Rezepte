using System.IO.Compression;
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

    /// <summary>
    /// The path of the zip archive containing the demo recipe images, relative to the application base directory.
    /// </summary>
    /// <returns>The demo images path.</returns>
    private static readonly string DemoImagesPath = Path.Combine(AppContext.BaseDirectory, "DemoData", "demo-images.zip");

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

        using var demoImages = OpenDemoImages(logger);

        var cookbookIds = await SeedCookbooksAsync(payload.UserId, cookbookService, ct).ConfigureAwait(false);
        var calendarRecipes = await SeedRecipesAsync(payload.UserId, cookbookIds, recipeService, demoImages, logger, ct).ConfigureAwait(false);
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
    /// <param name="demoImages">The demo images archive, or null when it is not available.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The demo/created recipe pairs of the calendar cookbook.</returns>
    private static async Task<List<SeededRecipe>> SeedRecipesAsync(
        string userId,
        IReadOnlyDictionary<string, string> cookbookIds,
        IRecipeService recipeService,
        ZipArchive? demoImages,
        ILogger logger,
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

                await AttachImageAsync(userId, recipe, demoRecipe, recipeService, demoImages, logger, ct).ConfigureAwait(false);

                if (cookbook.Name == DemoDataSource.CalendarCookbookName)
                {
                    calendarRecipes.Add(new SeededRecipe(demoRecipe, recipe));
                }
            }
        }

        return calendarRecipes;
    }

    /// <summary>
    /// Opens the demo images archive when it is present in the application output directory.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The open zip archive, or null when the archive does not exist.</returns>
    private static ZipArchive? OpenDemoImages(ILogger logger)
    {
        if (!File.Exists(DemoImagesPath))
        {
            logger.LogWarning("Demo images archive '{Path}' not found; seeding recipes without images", DemoImagesPath);
            return null;
        }

        return new ZipArchive(File.OpenRead(DemoImagesPath), ZipArchiveMode.Read);
    }

    /// <summary>
    /// Attaches the demo image matching the recipe title to the created recipe, when present in the archive.
    /// </summary>
    /// <param name="userId">The id of the user that owns the recipe.</param>
    /// <param name="recipe">The created recipe entity.</param>
    /// <param name="demoRecipe">The demo recipe definition.</param>
    /// <param name="recipeService">The recipe service.</param>
    /// <param name="demoImages">The demo images archive, or null when it is not available.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task AttachImageAsync(
        string userId,
        Recipe recipe,
        DemoRecipe demoRecipe,
        IRecipeService recipeService,
        ZipArchive? demoImages,
        ILogger logger,
        CancellationToken ct)
    {
        var entry = demoImages?.Entries.FirstOrDefault(e =>
            string.Equals(Path.GetFileNameWithoutExtension(e.Name), demoRecipe.Title, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return;
        }

        await using var imageStream = entry.Open();
        var (ok, error, _) = await recipeService.AddImageAsync(
            userId,
            recipe.Id,
            imageStream,
            entry.Name,
            GetImageContentType(entry.Name),
            ct).ConfigureAwait(false);
        if (!ok)
        {
            logger.LogWarning("Failed to attach demo image '{FileName}' to recipe '{Title}': {Error}", entry.Name, demoRecipe.Title, error);
        }
    }

    /// <summary>
    /// Gets the content type for an image file name based on its extension.
    /// </summary>
    /// <param name="fileName">The image file name.</param>
    /// <returns>The MIME content type.</returns>
    private static string GetImageContentType(string fileName)
        => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream",
        };

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
