namespace Rezepte.Import.Abstractions;

/// <summary>
/// Interaction surface used by import handlers to communicate with the user interface.
/// </summary>
public interface IImportInteraction
{
    /// <summary>
    /// Asks the user to confirm an action.
    /// </summary>
    /// <param name="prompt">Message shown to the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the user confirmed; otherwise <c>false</c>.</returns>
    Task<bool> AskForConfirmationAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    /// Reports a status message to the user.
    /// </summary>
    /// <param name="status">Status message to display.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReportStatusAsync(string status, CancellationToken ct = default);
}
