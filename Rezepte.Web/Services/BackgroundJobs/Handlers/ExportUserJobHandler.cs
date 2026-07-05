using System.Text.Json;
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
        var fileStore = scopeServices.GetRequiredService<ExportJobFileStore>();
        var logger = scopeServices.GetRequiredService<ILogger<ExportUserJobHandler>>();

        ExportJobPayload payload;
        try
        {
            payload = ExportJobPayload.FromJson(job.PayloadJson);
        }
        catch (JsonException je)
        {
            logger.LogWarning(je, "Invalid payload JSON for job {JobId}", job.Id);
            payload = new ExportJobPayload();
        }

        // Determine target user for export: prefer InitiatorUserId
        var userId = job.InitiatorUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Export job has no initiator user id.");
        }

        logger.LogInformation(
            "Starting export job {JobId} for user {UserId} includePdf={IncludePdf} includeImages={IncludeImages}",
            job.Id,
            userId,
            payload.IncludePdf,
            payload.IncludeImages);

        // Update progress and persist
        job.Progress = 5;
        job.ResultMessage = "Starting export";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Perform export (stream)
        await using var zipStream = await exportService.ExportUserAsync(userId, payload.IncludeImages, payload.IncludePdf, ct).ConfigureAwait(false);

        job.Progress = 60;
        job.ResultMessage = "Export created, saving file";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var fileName = fileStore.CreateSafeFileName("export", userId, job.Id);
        var filePath = fileStore.GetPathForFileName(fileName);

        // Persist to disk (could be replaced by blob storage)
        await using (var fs = File.Create(filePath))
        {
            zipStream.Seek(0, SeekOrigin.Begin);
            await zipStream.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        job.Progress = 90;
        job.ResultMessage = fileName;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Export job {JobId} finished, file saved to {FilePath}", job.Id, filePath);
        // Final state will be set in hosted service (Succeeded + Progress=100)
    }

}
