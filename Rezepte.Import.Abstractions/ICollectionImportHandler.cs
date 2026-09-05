namespace Rezepte.Import.Abstractions;

/// <summary>
/// Handles import of a recipe collection that is split into multiple items.
/// </summary>
public interface ICollectionImportHandler : IImportHandler
{
    /// <summary>
    /// Reads a preview of the collection without importing it.
    /// </summary>
    /// <param name="stream">Stream containing the collection data.</param>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="uri">Optional URI the data was loaded from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A preview of the collection, or <c>null</c> if no collection could be read.</returns>
    Task<ImportCollectionPreview?> TryReadCollectionPreviewAsync(Stream stream, string fileName, string? uri, CancellationToken ct = default);

    /// <summary>
    /// Imports a single item from a previously read collection.
    /// </summary>
    /// <param name="item">Item to import.</param>
    /// <param name="userId">Identifier of the user performing the import.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the import operation.</returns>
    Task<ImportResult> ImportCollectionItemAsync(ImportCollectionItem item, string userId, CancellationToken ct = default);
}
