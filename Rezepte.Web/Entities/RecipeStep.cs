namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the recipe step class.
/// </summary>
public class RecipeStep
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
    public int StepIndex { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int DurationMinutes { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool RequiresOvernightRest { get; set; }

    /// <summary>
    /// lists the value.
    /// </summary>
    /// <returns>The result. RecipeIngredient: The recipe ingredient type parameter.</returns>
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
