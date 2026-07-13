namespace Rezepte.Import.Abstractions;

public interface IInteractiveImportHandler : IImportHandler
{
    Task<ImportResult> HandleInteractiveAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, IImportInteraction interaction, CancellationToken ct = default);
}
