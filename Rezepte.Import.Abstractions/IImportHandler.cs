namespace Rezepte.Import.Abstractions;

/// <summary>
/// Handles import of a single recipe or a set of recipes from a stream.
/// </summary>
public interface IImportHandler
{
    /// <summary>
    /// Gets or sets the identifier of the user that owns the import.
    /// </summary>
    string UserId { set; }

    /// <summary>
    /// Determines whether this handler can process the provided stream.
    /// </summary>
    /// <param name="stream">Stream to inspect. The caller must reset it after the call.</param>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the handler can process the stream; otherwise <c>false</c>.</returns>
    Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Processes the provided stream and returns the import result.
    /// </summary>
    /// <param name="stream">Stream containing the data to import.</param>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="uri">Optional URI the data was loaded from.</param>
    /// <param name="targetCookbookId">Identifier of the cookbook to import into.</param>
    /// <param name="userId">Identifier of the user performing the import.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the import operation.</returns>
    Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default);
}
