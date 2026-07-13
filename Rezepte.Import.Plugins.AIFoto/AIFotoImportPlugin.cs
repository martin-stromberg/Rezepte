using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.AIFoto;

public sealed class AIFotoImportPlugin : IImportPlugin
{
    public string Id => "ai-foto";
    public string DisplayName => "AI-Foto";
    public string? Description => "Importiert Rezepte aus Fotos per KI.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(AIFotoImportHandler);
}

public sealed class AIFotoImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "AI-Foto plugin parser is not bundled in this project yet.", []));
}
