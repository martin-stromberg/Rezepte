using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Rezepte.Web.Services.Import
{
    public sealed class TestRecipeImportService : ITestRecipeImportService
    {
        private readonly string _filePath;
        private readonly ILogger<TestRecipeImportService> _logger;

        public TestRecipeImportService(IHostEnvironment env, ILogger<TestRecipeImportService> logger)
        {
            _logger = logger;
            // Datei im Programmverzeichnis / ContentRoot (Ausgabeverzeichnis)
            _filePath = Path.Combine(env.ContentRootPath ?? AppContext.BaseDirectory, "test.recipe-import.json");
        }

        public async Task<string[]> GetTestUrlsAsync(CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(_filePath)) return Array.Empty<string>();

                await using var fs = File.OpenRead(_filePath);
                var urls = await JsonSerializer.DeserializeAsync<string[]>(fs, cancellationToken: ct).ConfigureAwait(false);
                return urls ?? Array.Empty<string>();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Einlesen der Test-URL-Datei '{Path}'", _filePath);
                return Array.Empty<string>();
            }
        }

        public async Task<bool> HasTestUrlsAsync(CancellationToken ct = default)
        {
            var urls = await GetTestUrlsAsync(ct).ConfigureAwait(false);
            return urls.Length > 0;
        }
    }
}