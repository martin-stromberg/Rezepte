using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using System.Buffers.Text;
using System.Security.Cryptography;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the recipe share service class.
/// </summary>
public class RecipeShareService : IRecipeShareService
{
    private readonly RezepteDbContext _db;
    private readonly ISettingsService _settings;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeShareService"/> class.
    /// </summary>
    /// <param name="db">The db parameter.</param>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="timeProvider">The time provider parameter.</param>
    public RecipeShareService(RezepteDbContext db, ISettingsService settings, TimeProvider timeProvider)
    {
        _db = db;
        _settings = settings;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates the share link async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="includeSideDishes">The include side dishes parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<RecipeShareLink?> CreateShareLinkAsync(string userId, string recipeId, bool includeSideDishes, CancellationToken ct = default)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, ct);
        if (recipe is null) return null;

        var validityDays = await _settings.GetShareLinkValidityDaysAsync(ct);
        var now = _timeProvider.GetUtcNow();
        var link = new RecipeShareLink
        {
            Token = GenerateToken(),
            RecipeId = recipe.Id,
            UserId = userId,
            IncludeSideDishes = includeSideDishes,
            CreatedAt = now.UtcDateTime,
            ExpiresAt = now.UtcDateTime.AddDays(validityDays)
        };
        _db.RecipeShareLinks.Add(link);
        await _db.SaveChangesAsync(ct);
        return link;
    }

    /// <summary>
    /// Gets the shared recipe async.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<SharedRecipeView?> GetSharedRecipeAsync(string token, CancellationToken ct = default)
    {
        var link = await LoadValidLinkAsync(token, ct);
        if (link is null) return null;

        var recipe = await LoadRecipeAsync(link.RecipeId, ct);
        if (recipe is null) return null;

        return ToView(recipe, link, link.IncludeSideDishes);
    }

    /// <summary>
    /// Gets the shared side dish async.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<SharedRecipeView?> GetSharedSideDishAsync(string token, string recipeId, CancellationToken ct = default)
    {
        var link = await LoadValidLinkAsync(token, ct);
        if (link is null || !link.IncludeSideDishes) return null;

        var isDirectSideDish = await _db.RecipeSideDishes
            .AnyAsync(sd => sd.RecipeId == link.RecipeId && sd.SideDishRecipeId == recipeId, ct);
        if (!isDirectSideDish) return null;

        var recipe = await LoadRecipeAsync(recipeId, ct);
        if (recipe is null) return null;

        // Only direct side dishes are shared: the side dish's own side dishes stay hidden.
        return ToView(recipe, link, includeSideDishes: false);
    }

    /// <summary>
    /// Gets the shared image async.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="imageId">The image id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<RecipeImage?> GetSharedImageAsync(string token, string imageId, CancellationToken ct = default)
    {
        var link = await LoadValidLinkAsync(token, ct);
        if (link is null) return null;

        var allowedRecipeIds = new List<string> { link.RecipeId };
        if (link.IncludeSideDishes)
        {
            var sideDishIds = await _db.RecipeSideDishes
                .Where(sd => sd.RecipeId == link.RecipeId)
                .Select(sd => sd.SideDishRecipeId)
                .ToListAsync(ct);
            allowedRecipeIds.AddRange(sideDishIds);
        }

        return await _db.RecipeImages
            .FirstOrDefaultAsync(i => i.Id == imageId && allowedRecipeIds.Contains(i.RecipeId), ct);
    }

    private async Task<RecipeShareLink?> LoadValidLinkAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var link = await _db.RecipeShareLinks.FirstOrDefaultAsync(l => l.Token == token, ct);
        if (link is null) return null;
        if (link.ExpiresAt <= _timeProvider.GetUtcNow().UtcDateTime) return null;
        return link;
    }

    private async Task<Recipe?> LoadRecipeAsync(string recipeId, CancellationToken ct)
    {
        return await _db.Recipes
            .Include(r => r.Steps).ThenInclude(s => s.Ingredients)
            .Include(r => r.Images)
            .Include(r => r.SideDishes).ThenInclude(sd => sd.SideDishRecipe).ThenInclude(r => r!.Images)
            .FirstOrDefaultAsync(r => r.Id == recipeId, ct);
    }

    private static SharedRecipeView ToView(Recipe recipe, RecipeShareLink link, bool includeSideDishes)
    {
        var imageUrls = recipe.Images
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => ImageUrl(link.Token, i.Id))
            .ToList();

        var steps = recipe.Steps
            .OrderBy(s => s.StepIndex)
            .Select(s => new SharedStepView(
                s.Title,
                s.Description,
                s.DurationMinutes,
                s.RequiresOvernightRest,
                s.Ingredients
                    .Select(i => new SharedIngredientView(i.Amount, i.Unit, i.Name))
                    .ToList()))
            .ToList();

        var sideDishes = includeSideDishes
            ? recipe.SideDishes
                .Where(sd => sd.SideDishRecipe is not null)
                .OrderBy(sd => sd.OrderIndex)
                .ThenBy(sd => sd.SideDishRecipe!.Title)
                .Select(sd => new SharedSideDishView(
                    sd.SideDishRecipeId,
                    sd.SideDishRecipe!.Title,
                    sd.SideDishRecipe.Portions,
                    sd.SideDishRecipe.Images
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => ImageUrl(link.Token, i.Id))
                        .FirstOrDefault(),
                    $"/share/{link.Token}/sidedish/{sd.SideDishRecipeId}"))
                .ToList()
            : new List<SharedSideDishView>();

        return new SharedRecipeView(recipe.Title, recipe.Description, recipe.Portions, recipe.Uri, imageUrls, steps, sideDishes);
    }

    private static string ImageUrl(string token, string imageId) => $"/api/share/{token}/image/{imageId}";

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64Url.EncodeToString(bytes);
    }
}
