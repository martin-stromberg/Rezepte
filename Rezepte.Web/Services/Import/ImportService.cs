using Microsoft.Extensions.Logging;
using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Web.Services.Import;

public class ImportService(IPluginManager pluginManager, IServiceProvider serviceProvider, ILogger<ImportService> logger) : IImportService
{
    private readonly IPluginManager _pluginManager = pluginManager;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<ImportService> _logger = logger;

    public async Task<ImportResult> ImportAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

        _logger.LogInformation("Import requested: {FileName} -> cookbook {CookbookId} by user {UserId}", fileName, targetCookbookId, userId);

        // Try each handler: reset stream position for each attempt
        var handlers = await _pluginManager.GetActiveHandlersAsync(_serviceProvider, ct).ConfigureAwait(false);
        foreach (var pluginHandler in handlers)
        {
            var handler = pluginHandler.Handler;
            stream.Seek(0, SeekOrigin.Begin);
            bool can;
            try
            {
                can = await handler.CanHandleAsync(stream, fileName, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Plugin {PluginId} handler {Handler} CanHandleAsync failed for {FileName}", pluginHandler.Plugin.Id, handler.GetType().Name, fileName);
                continue;
            }

            if (!can) continue;

            stream.Seek(0, SeekOrigin.Begin);
            try
            {
                var res = await handler.HandleAsync(stream, fileName, null, targetCookbookId, userId, ct).ConfigureAwait(false);
                _logger.LogInformation("Import handled by plugin {PluginId} handler {Handler}, success={Success}", pluginHandler.Plugin.Id, handler.GetType().Name, res.Success);
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {PluginId} handler {Handler} failed during import", pluginHandler.Plugin.Id, handler.GetType().Name);
                return new ImportResult(false, ex.Message, new List<string>());
            }
        }

        return new ImportResult(false, "No suitable import plugin found for this file or URL.", new List<string>());
    }
}
