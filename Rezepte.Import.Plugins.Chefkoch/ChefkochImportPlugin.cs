using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.Chefkoch;

public sealed class ChefkochImportPlugin : IImportPlugin
{
    public string Id => "chefkoch";
    public string DisplayName => "Chefkoch";
    public string? Description => "Importiert Rezepte von Chefkoch.";
    public string Version => "0.0.0-placeholder";
    public Type HandlerType => typeof(ChefkochImportHandler);
}

public sealed class ChefkochImportHandler : IImportHandler
{
    public string UserId { private get; set; } = string.Empty;
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default) => Task.FromResult(false);
    public Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default) => Task.FromResult(new ImportResult(false, "Chefkoch plugin parser is not bundled in this project yet.", []));
}
