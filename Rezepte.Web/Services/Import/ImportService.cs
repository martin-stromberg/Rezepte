using Microsoft.Extensions.Logging;

namespace Rezepte.Web.Services.Import;

public class ImportService(IEnumerable<IImportHandler> handlers, ILogger<ImportService> logger) : IImportService
{
    private readonly IEnumerable<IImportHandler> _handlers = handlers;
    private readonly ILogger<ImportService> _logger = logger;

    public async Task<ImportResult> ImportAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

        _logger.LogInformation("Import requested: {FileName} -> cookbook {CookbookId} by user {UserId}", fileName, targetCookbookId, userId);

        // Try each handler: reset stream position for each attempt
        foreach (var handler in _handlers)
        {
            stream.Seek(0, SeekOrigin.Begin);
            bool can;
            try
            {
                can = await handler.CanHandleAsync(stream, fileName, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Handler {Handler} CanHandleAsync failed for {FileName}", handler.GetType().Name, fileName);
                continue;
            }

            if (!can) continue;

            stream.Seek(0, SeekOrigin.Begin);
            try
            {
                var res = await handler.HandleAsync(stream, fileName, null, targetCookbookId, userId, ct).ConfigureAwait(false);
                _logger.LogInformation("Import handled by {Handler}, success={Success}", handler.GetType().Name, res.Success);
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed during import", handler.GetType().Name);
                return new ImportResult(false, ex.Message, new List<string>());
            }
        }

        return new ImportResult(false, "No suitable import handler found for this file.", new List<string>());
    }
}