namespace Rezepte.Import.Abstractions;

/// <summary>
/// Metadata and factory contract for a recipe import plugin.
/// </summary>
public interface IImportPlugin
{
    /// <summary>
    /// Unique identifier of the plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in the user interface.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Optional longer description of the plugin.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Version of the plugin.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Type of the handler that performs the import.
    /// </summary>
    Type HandlerType { get; }

    /// <summary>
    /// Default priority used when multiple plugins can handle the same input.
    /// Higher values are evaluated first.
    /// </summary>
    int DefaultPriority => 0;

    /// <summary>
    /// Checks whether the plugin can be used in the current environment.
    /// </summary>
    /// <param name="serviceProvider">Application service provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating usability and any issues.</returns>
    Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        => Task.FromResult(PluginUsabilityResult.Usable);
}
