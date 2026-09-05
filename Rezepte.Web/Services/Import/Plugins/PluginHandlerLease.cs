namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the handler lease.
/// </summary>
/// <param name="handlers">The handlers parameter.</param>
/// <param name="releaser">The releaser parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginHandlerLease(IReadOnlyList<PluginImportHandler> handlers, IAsyncDisposable? releaser) : IAsyncDisposable
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public IReadOnlyList<PluginImportHandler> Handlers { get; } = handlers;

    /// <summary>
    /// disposes the async.
    /// </summary>
    public ValueTask DisposeAsync() => releaser?.DisposeAsync() ?? ValueTask.CompletedTask;
}
