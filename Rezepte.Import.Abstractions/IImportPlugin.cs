namespace Rezepte.Import.Abstractions;

public interface IImportPlugin
{
    string Id { get; }
    string DisplayName { get; }
    string? Description { get; }
    string Version { get; }
    Type HandlerType { get; }
    int DefaultPriority => 0;

    Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => Task.FromResult(PluginUsabilityResult.Usable);
}
