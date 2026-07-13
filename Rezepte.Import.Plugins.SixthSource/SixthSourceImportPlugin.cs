using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.SixthSource;

public sealed class SixthSourceImportPlugin : IImportPlugin
{
    public string Id => "sixth-source";
    public string DisplayName => "SixthSource";
    public string? Description => "Importiert Rezepte der sechsten URL-Quelle.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(SixthSourceImportHandler);
}

public sealed class SixthSourceImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "SixthSource plugin parser is not bundled in this project yet.", []));
}
