using Rezepte.Import.Abstractions;

namespace Rezepte.Web.Services.Import;

public interface IImportedRecipePersister
{
    Task<ImportResult> PersistAsync(ImportResult result, string targetCookbookId, string userId, CancellationToken ct = default);
}
