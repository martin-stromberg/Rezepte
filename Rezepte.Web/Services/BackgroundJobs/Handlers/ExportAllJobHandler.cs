using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;

namespace Rezepte.Web.Services.BackgroundJobs.Handlers;

public class ExportAllJobHandler : IBackgroundJobHandler
{
    public string JobType => "export:all";

    public async Task HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(job);
        ct.ThrowIfCancellationRequested();

        var db = scopeServices.GetRequiredService<RezepteDbContext>();
        var exportService = scopeServices.GetRequiredService<IExportService>();
        var fileStore = scopeServices.GetRequiredService<ExportJobFileStore>();
        var logger = scopeServices.GetRequiredService<ILogger<ExportAllJobHandler>>();
        var payload = ExportJobPayload.FromJson(job.PayloadJson);

        var adminId = job.InitiatorUserId;
        if (string.IsNullOrWhiteSpace(adminId))
        {
            throw new InvalidOperationException("Admin export job has no initiator user id.");
        }

        logger.LogInformation(
            "Starting admin export job {JobId} for admin {AdminId} includePdf={IncludePdf} includeImages={IncludeImages}",
            job.Id,
            adminId,
            payload.IncludePdf,
            payload.IncludeImages);

        job.Progress = 5;
        job.ResultMessage = "Export wird vorbereitet";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await using var zipStream = await exportService
            .ExportAllAsync(adminId, payload.IncludeImages, payload.IncludePdf, ct)
            .ConfigureAwait(false);

        job.Progress = 70;
        job.ResultMessage = "Export erstellt, Datei wird gespeichert";
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var fileName = fileStore.CreateSafeFileName("export-all", adminId, job.Id);
        var filePath = fileStore.GetPathForFileName(fileName);

        logger.LogInformation(
            "Saving admin export {JobId} to file {FileName}",
            job.Id,
            fileName);

        long fileSize;
        await using (var fs = File.Create(filePath))
        {
            zipStream.Seek(0, SeekOrigin.Begin);
            await zipStream.CopyToAsync(fs, ct).ConfigureAwait(false);
            fileSize = fs.Length;
        }

        logger.LogInformation(
            "Admin export {JobId} written to {FileName} ({FileSize} bytes)",
            job.Id,
            fileName,
            fileSize);

        db.UserExportFiles.Add(new Rezepte.Web.Entities.UserExportFile
        {
            UserId = adminId,
            IsAdminExport = true,
            FileName = fileName,
            Size = fileSize,
            JobId = job.Id
        });

        job.Progress = 90;
        job.ResultMessage = fileName;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Admin export job {JobId} finished, file {FileName} ({FileSize} bytes) registered",
            job.Id,
            fileName,
            fileSize);
    }
}
