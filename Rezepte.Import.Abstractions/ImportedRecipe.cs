namespace Rezepte.Import.Abstractions;

/// <summary>
/// Recipe imported by a plugin before it is persisted.
/// </summary>
public sealed record ImportedRecipe
{
    /// <summary>
    /// Title of the recipe.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Description of the recipe.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// URI the recipe was imported from.
    /// </summary>
    public string? SourceUri { get; init; }

    /// <summary>
    /// Number of portions the recipe is designed for.
    /// </summary>
    public int Portions { get; init; }

    /// <summary>
    /// Estimated work time in minutes.
    /// </summary>
    public int WorkTimeMinutes { get; init; }

    /// <summary>
    /// Ingredients of the recipe.
    /// </summary>
    public IReadOnlyList<ImportedIngredient> Ingredients { get; init; } = [];

    /// <summary>
    /// Preparation steps of the recipe.
    /// </summary>
    public IReadOnlyList<ImportedRecipeStep> Steps { get; init; } = [];

    /// <summary>
    /// Images associated with the recipe.
    /// </summary>
    public IReadOnlyList<ImportedImage> Images { get; init; } = [];
}
