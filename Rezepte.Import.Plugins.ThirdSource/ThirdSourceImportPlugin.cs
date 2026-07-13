using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.ThirdSource;

public sealed class ThirdSourceImportPlugin : IImportPlugin
{
    public string Id => "third-source";
    public string DisplayName => "ThirdSource";
    public string? Description => "Importiert Rezepte der dritten URL-Quelle.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(ThirdSourceImportHandler);
}

public sealed class ThirdSourceImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "ThirdSource plugin parser is not bundled in this project yet.", []));
}
