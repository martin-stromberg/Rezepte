using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.SecondSource;

public sealed class SecondSourceImportPlugin : IImportPlugin
{
    public string Id => "second-source";
    public string DisplayName => "SecondSource";
    public string? Description => "Importiert Rezepte der zweiten URL-Quelle.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(SecondSourceImportHandler);
}

public sealed class SecondSourceImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "SecondSource plugin parser is not bundled in this project yet.", []));
}
