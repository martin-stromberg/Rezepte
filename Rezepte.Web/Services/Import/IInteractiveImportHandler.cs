namespace Rezepte.Web.Services.Import;

/// <summary>
/// Optional: import handler that can ask the orchestrator for interactive confirmations.
/// Handlers that need user confirmation implement this interface.
/// </summary>
public interface IInteractiveImportHandler : IImportHandler
{
    /// <summary>
    /// Handle the import interactively. The orchestrator provides an interaction object
    /// that the handler can use to ask for confirmation from the user.
    /// </summary>
    Task<ImportResult> HandleInteractiveAsync(Stream stream, string fileName, string targetCookbookId, string userId, IImportInteraction interaction, CancellationToken ct = default);
}
