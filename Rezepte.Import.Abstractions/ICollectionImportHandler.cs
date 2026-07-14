namespace Rezepte.Import.Abstractions;

public interface ICollectionImportHandler : IImportHandler
{
    Task<ImportCollectionPreview?> TryReadCollectionPreviewAsync(Stream stream, string fileName, string? uri, CancellationToken ct = default);

    Task<ImportResult> ImportCollectionItemAsync(ImportCollectionItem item, string userId, CancellationToken ct = default);
}
