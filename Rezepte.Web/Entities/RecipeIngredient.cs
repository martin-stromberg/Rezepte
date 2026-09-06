namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the recipe ingredient class.
/// </summary>
public class RecipeIngredient
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string StepId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Unit { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
