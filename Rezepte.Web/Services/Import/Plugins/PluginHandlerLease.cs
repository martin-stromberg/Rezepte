namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginHandlerLease(IReadOnlyList<PluginImportHandler> handlers, IAsyncDisposable? releaser) : IAsyncDisposable
{
    public IReadOnlyList<PluginImportHandler> Handlers { get; } = handlers;

    public ValueTask DisposeAsync() => releaser?.DisposeAsync() ?? ValueTask.CompletedTask;
}
