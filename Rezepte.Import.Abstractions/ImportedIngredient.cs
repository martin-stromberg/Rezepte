namespace Rezepte.Import.Abstractions;

/// <summary>
/// Ingredient imported as part of a recipe.
/// </summary>
public sealed record ImportedIngredient
{
    /// <summary>
    /// Quantity text of the ingredient, for example "200 g".
    /// </summary>
    public string? Quantity { get; init; }

    /// <summary>
    /// Name of the ingredient.
    /// </summary>
    public string? Name { get; init; }
}
