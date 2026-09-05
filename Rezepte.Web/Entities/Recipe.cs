namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the recipe class.
/// </summary>
public class Recipe
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="RecipeStep">The recipe step type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="RecipeImage">The recipe image type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<RecipeImage> Images { get; set; } = new List<RecipeImage>();
    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="RecipeCookbook">The recipe cookbook type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<RecipeCookbook> RecipeCookbooks { get; set; } = new List<RecipeCookbook>();
    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="RecipeSideDish">The recipe side dish type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<RecipeSideDish> SideDishes { get; set; } = new List<RecipeSideDish>();
    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="RecipeSideDish">The recipe side dish type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<RecipeSideDish> UsedAsSideDishFor { get; set; } = new List<RecipeSideDish>();
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Uri { get; internal set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int Portions { get; set; } = 0;
}
