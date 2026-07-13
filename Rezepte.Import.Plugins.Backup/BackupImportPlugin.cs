using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.Backup;

public sealed class BackupImportPlugin : IImportPlugin
{
    public string Id => "backup";
    public string DisplayName => "Backup";
    public string? Description => "Importiert Backup-ZIP-Dateien.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(BackupImportHandler);
}

public sealed class BackupImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "Backup plugin parser is not bundled in this project yet.", []));
}
