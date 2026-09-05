namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the recipe side dish class.
/// </summary>
public class RecipeSideDish
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string RecipeId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string SideDishRecipeId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Recipe? Recipe { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Recipe? SideDishRecipe { get; set; }
}
