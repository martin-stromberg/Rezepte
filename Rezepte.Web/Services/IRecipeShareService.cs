using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

/// <summary>
/// Defines the irecipe share service interface.
/// </summary>
public interface IRecipeShareService
{
    /// <summary>
    /// Creates the share link async.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="includeSideDishes">The include side dishes parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<RecipeShareLink?> CreateShareLinkAsync(string userId, string recipeId, bool includeSideDishes, CancellationToken ct = default);
    /// <summary>
    /// Gets the shared recipe async.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<SharedRecipeView?> GetSharedRecipeAsync(string token, CancellationToken ct = default);
    /// <summary>
    /// Gets the shared side dish async.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<SharedRecipeView?> GetSharedSideDishAsync(string token, string recipeId, CancellationToken ct = default);
    /// <summary>
    /// Gets the shared image async.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="imageId">The image id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<RecipeImage?> GetSharedImageAsync(string token, string imageId, CancellationToken ct = default);
}

/// <summary>
/// shareds the recipe view.
/// </summary>
/// <param name="Title">The title parameter.</param>
/// <param name="Description">The description parameter.</param>
/// <param name="Portions">The portions parameter.</param>
/// <param name="SourceUri">The source uri parameter.</param>
/// <param name="ImageUrls">The image urls parameter.</param>
/// <param name="Steps">The steps parameter.</param>
/// <param name="SideDishes">The side dishes parameter.</param>
/// <returns>The result.</returns>
public record SharedRecipeView(string Title, string? Description, int Portions, string? SourceUri, List<string> ImageUrls, List<SharedStepView> Steps, List<SharedSideDishView> SideDishes);

/// <summary>
/// shareds the step view.
/// </summary>
/// <param name="Title">The title parameter.</param>
/// <param name="Description">The description parameter.</param>
/// <param name="DurationMinutes">The duration minutes parameter.</param>
/// <param name="RequiresOvernightRest">The requires overnight rest parameter.</param>
/// <param name="Ingredients">The ingredients parameter.</param>
/// <returns>The result.</returns>
public record SharedStepView(string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, List<SharedIngredientView> Ingredients);

/// <summary>
/// shareds the ingredient view.
/// </summary>
/// <param name="Amount">The amount parameter.</param>
/// <param name="Unit">The unit parameter.</param>
/// <param name="Name">The name parameter.</param>
/// <returns>The result.</returns>
public record SharedIngredientView(decimal Amount, string? Unit, string Name);

/// <summary>
/// shareds the side dish view.
/// </summary>
/// <param name="RecipeId">The recipe id parameter.</param>
/// <param name="Title">The title parameter.</param>
/// <param name="Portions">The portions parameter.</param>
/// <param name="ImageUrl">The image url parameter.</param>
/// <param name="Url">The url parameter.</param>
/// <returns>The result.</returns>
public record SharedSideDishView(string RecipeId, string Title, int Portions, string? ImageUrl, string Url);
