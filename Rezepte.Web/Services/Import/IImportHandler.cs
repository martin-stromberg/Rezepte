namespace Rezepte.Web.Services.Import;

public record ImportResult(bool Success, string? Error, List<string> CreatedRecipeIds);

public interface IImportHandler
{
    /// <summary>
    /// Prüft schnell, ob dieser Handler mit der gegebenen Datei umgehen kann.
    /// </summary>
    Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Führe den Import aus. Gibt CreatedRecipeIds zurück.
    /// </summary>
    Task<ImportResult> HandleAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default);
}