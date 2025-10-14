using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Services.BackgroundJobs.Handlers;

public class ExportUserJobHandler : IBackgroundJobHandler
{
    public string JobType => "export:user";

    public async Task HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct)
    {
        if (job is null) throw new ArgumentNullException(nameof(job));
        ct.ThrowIfCancellationRequested();

        // Resolve scoped services from the provided IServiceProvider (same scope as HostedService created)
        var db = scopeServices.GetRequiredService<RezepteDbContext>();
        var exportService = scopeServices.GetRequiredService<IExportService>();
        var env = scopeServices.GetRequiredService<IWebHostEnvironment>();
        var logger = scopeServices.GetRequiredService<ILogger<ExportUserJobHandler>>();

        // Parse payload (optional)
        bool includePdf = false;
        try
        {
            if (!string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                using var doc = JsonDocument.Parse(job.PayloadJson);
                if (doc.RootElement.TryGetProperty("includePdf", out var p) && p.ValueKind == JsonValueKind.True)
                {
                    includePdf = true;
                }
            }
        }
        catch (JsonException je)
        {
            logger.LogWarning(je, "Invalid payload JSON for job {JobId}", job.Id);
            // continue with defaults
        }

        // Determine target user for export: prefer InitiatorUserId
        var userId = job.InitiatorUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Export job has no initiator user id.");
        }

        logger.LogInformation("Starting export job {JobId} for user {UserId} includePdf={IncludePdf}", job.Id, userId, includePdf);

        // Update progress and persist
        job.Progress = 5;
        job.ResultMessage = "Starting export";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Perform export (stream)
        await using var zipStream = await exportService.ExportUserAsync(userId, true, includePdf, ct).ConfigureAwait(false);

        job.Progress = 60;
        job.ResultMessage = "Export created, saving file";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Ensure exports folder exists (content root / exports)
        var exportsDir = Path.Combine(env.ContentRootPath, "exports");
        Directory.CreateDirectory(exportsDir);

        var fileName = SanitizeFileName($"export-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{job.Id}.zip");
        var filePath = Path.Combine(exportsDir, fileName);

        // Persist to disk (could be replaced by blob storage)
        await using (var fs = File.Create(filePath))
        {
            zipStream.Seek(0, SeekOrigin.Begin);
            await zipStream.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        // Update job with result path (relative) and progress
        job.Progress = 90;
        job.ResultMessage = $"file://{filePath}";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Export job {JobId} finished, file saved to {FilePath}", job.Id, filePath);
        // Final state will be set in hosted service (Succeeded + Progress=100)
    }

    private static string SanitizeFileName(string input)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }
        return input;
    }
}