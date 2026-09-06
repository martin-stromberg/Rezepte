namespace Rezepte.Import.Abstractions;

/// <summary>
/// Handles imports that require user interaction during processing.
/// </summary>
public interface IInteractiveImportHandler : IImportHandler
{
    /// <summary>
    /// Processes the provided stream interactively.
    /// </summary>
    /// <param name="stream">Stream containing the data to import.</param>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="uri">Optional URI the data was loaded from.</param>
    /// <param name="targetCookbookId">Identifier of the cookbook to import into.</param>
    /// <param name="userId">Identifier of the user performing the import.</param>
    /// <param name="interaction">Interaction surface for user communication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the import operation.</returns>
    Task<ImportResult> HandleInteractiveAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, IImportInteraction interaction, CancellationToken ct = default);
}
