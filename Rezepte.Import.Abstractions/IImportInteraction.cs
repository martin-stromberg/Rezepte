namespace Rezepte.Import.Abstractions;

public interface IImportInteraction
{
    Task<bool> AskForConfirmationAsync(string prompt, CancellationToken ct = default);

    Task ReportStatusAsync(string status, CancellationToken ct = default);
}
