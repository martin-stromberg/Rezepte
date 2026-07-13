using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.FourthSource;

public sealed class FourthSourceImportPlugin : IImportPlugin
{
    public string Id => "fourth-source";
    public string DisplayName => "FourthSource";
    public string? Description => "Importiert Rezepte der vierten URL-Quelle.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(FourthSourceImportHandler);
}

public sealed class FourthSourceImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "FourthSource plugin parser is not bundled in this project yet.", []));
}
