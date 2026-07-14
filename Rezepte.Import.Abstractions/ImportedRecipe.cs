namespace Rezepte.Import.Abstractions;

public sealed record ImportedRecipe
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? SourceUri { get; init; }
    public int Portions { get; init; }
    public int WorkTimeMinutes { get; init; }
    public IReadOnlyList<ImportedIngredient> Ingredients { get; init; } = [];
    public IReadOnlyList<ImportedRecipeStep> Steps { get; init; } = [];
    public IReadOnlyList<ImportedImage> Images { get; init; } = [];
}
