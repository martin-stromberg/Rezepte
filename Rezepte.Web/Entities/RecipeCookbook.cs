namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the recipe cookbook class.
/// </summary>
public class RecipeCookbook
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string CookbookId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Cookbook Cookbook { get; set; } = null!;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string RecipeId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Recipe Recipe { get; set; } = null!;
}
