using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.AIUrl;

public sealed class AIUrlImportPlugin : IImportPlugin
{
    public string Id => "ai-url";
    public string DisplayName => "AI-URL";
    public string? Description => "Importiert Rezepte aus Webseiten per KI.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(AIUrlImportHandler);
}

public sealed class AIUrlImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "AI-URL plugin parser is not bundled in this project yet.", []));
}
