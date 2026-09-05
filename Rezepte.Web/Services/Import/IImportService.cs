namespace Rezepte.Web.Services.Import;

/// <summary>
/// Defines the iimport service interface.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// imports the async.
    /// </summary>
    /// <param name="stream">The stream parameter.</param>
    /// <param name="fileName">The file name parameter.</param>
    /// <param name="targetCookbookId">The target cookbook id parameter.</param>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<ImportResult> ImportAsync(Stream stream, string fileName, string? targetCookbookId, string userId, CancellationToken ct = default);
}
