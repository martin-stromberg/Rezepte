namespace Rezepte.Web.Services.Import;

public interface IImportService
{
    Task<ImportResult> ImportAsync(Stream stream, string fileName, string? targetCookbookId, string userId, CancellationToken ct = default);
}

