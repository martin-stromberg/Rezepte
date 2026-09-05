namespace Rezepte.Import.Abstractions;

/// <summary>
/// Result of a recipe import operation.
/// </summary>
/// <param name="Success"><c>true</c> if the import completed successfully; otherwise <c>false</c>.</param>
/// <param name="Error">Optional error message when the import failed.</param>
/// <param name="CreatedRecipeIds">Identifiers of recipes created by the import.</param>
/// <param name="ImportedRecipes">Optional collection of imported recipes.</param>
/// <returns>A new instance of the <see cref="ImportResult"/> record.</returns>
public record ImportResult(
    bool Success,
    string? Error,
    List<string> CreatedRecipeIds,
    IReadOnlyList<ImportedRecipe>? ImportedRecipes = null);
