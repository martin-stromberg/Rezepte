namespace Rezepte.Import.Abstractions;

public record ImportResult(
    bool Success,
    string? Error,
    List<string> CreatedRecipeIds,
    IReadOnlyList<ImportedRecipe>? ImportedRecipes = null);
