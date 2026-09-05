namespace Rezepte.Import.Abstractions;

/// <summary>
/// Preparation step of an imported recipe.
/// </summary>
public sealed record ImportedRecipeStep
{
    /// <summary>
    /// Text describing the preparation step.
    /// </summary>
    public string? Text { get; init; }
}
