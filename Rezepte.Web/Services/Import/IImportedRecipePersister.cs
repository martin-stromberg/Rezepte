using Rezepte.Import.Abstractions;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Defines the iimported recipe persister interface.
/// </summary>
public interface IImportedRecipePersister
{
    /// <summary>
    /// persists the async.
    /// </summary>
    /// <param name="result">The result parameter.</param>
    /// <param name="targetCookbookId">The target cookbook id parameter.</param>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<ImportResult> PersistAsync(ImportResult result, string? targetCookbookId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Initializes a new instance of the <see cref="Task"/> class.
    /// </summary>
    /// <param name="imported">The imported parameter.</param>
    /// <param name="targetCookbookId">The target cookbook id parameter.</param>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    /// <param name="Success">The success parameter.</param>
    /// <param name="Error">The error parameter.</param>
    /// <param name="RecipeId">The recipe id parameter.</param>
    Task<(bool Success, string? Error, string? RecipeId)> PersistRecipeAsync(ImportedRecipe imported, string? targetCookbookId, string userId, CancellationToken ct = default);
}
