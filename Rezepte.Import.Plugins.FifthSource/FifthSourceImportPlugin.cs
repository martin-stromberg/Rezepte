using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.FifthSource;

public sealed class FifthSourceImportPlugin : IImportPlugin
{
    public string Id => "fifth-source";
    public string DisplayName => "FifthSource";
    public string? Description => "Importiert Rezepte der fuenften URL-Quelle.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(FifthSourceImportHandler);
}

public sealed class FifthSourceImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "FifthSource plugin parser is not bundled in this project yet.", []));
}
