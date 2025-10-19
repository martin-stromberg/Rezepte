namespace Rezepte.Web.Services.Import;

/// <summary>
/// Orchestrator -> Handler interaction surface.
/// Handler can ask for confirmation which the orchestrator will surface to the caller/UI.
/// </summary>
public interface IImportInteraction
{
    /// <summary>
    /// Ask the user for confirmation with a prompt. Returns true when user confirmed, false when rejected.
    /// This method will complete only after client posted the confirmation.
    /// </summary>
    Task<bool> AskForConfirmationAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    /// Report a status message (e.g. "Checking handler X", "Downloading file...").
    /// Orchestrator will store this so UI can poll it.
    /// </summary>
    Task ReportStatusAsync(string status, CancellationToken ct = default);
}