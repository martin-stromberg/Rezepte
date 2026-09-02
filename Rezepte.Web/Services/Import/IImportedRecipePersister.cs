using Rezepte.Import.Abstractions;

namespace Rezepte.Web.Services.Import;

public interface IImportedRecipePersister
{
    Task<ImportResult> PersistAsync(ImportResult result, string? targetCookbookId, string userId, CancellationToken ct = default);

    Task<(bool Success, string? Error, string? RecipeId)> PersistRecipeAsync(ImportedRecipe imported, string? targetCookbookId, string userId, CancellationToken ct = default);
}
