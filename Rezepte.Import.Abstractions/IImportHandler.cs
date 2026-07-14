namespace Rezepte.Import.Abstractions;

public interface IImportHandler
{
    string UserId { set; }

    Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default);

    Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default);
}
