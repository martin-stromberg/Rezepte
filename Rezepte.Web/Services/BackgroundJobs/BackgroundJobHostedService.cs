using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;

namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// HostedService that processes queued BackgroundJobs.
/// It creates a scope per job execution and resolves registered IBackgroundJobHandler(s).
/// </summary>
public class BackgroundJobHostedService : BackgroundService
{
    private readonly BackgroundJobQueue _queue;
    private readonly IServiceProvider _svcProvider;
    private readonly ILogger<BackgroundJobHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundJobHostedService"/> class.
    /// </summary>
    /// <param name="queue">The queue parameter.</param>
    /// <param name="svcProvider">The svc provider parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    public BackgroundJobHostedService(BackgroundJobQueue queue, IServiceProvider svcProvider, ILogger<BackgroundJobHostedService> logger)
    {
        _queue = queue;
        _svcProvider = svcProvider;
        _logger = logger;
    }

    /// <summary>
    /// executes the async.
    /// </summary>
    /// <param name="stoppingToken">The stopping token parameter.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobHostedService starting.");
        var reader = _queue.Reader;

        try
        {
            await foreach (var jobId in reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                if (stoppingToken.IsCancellationRequested) break;
                await ProcessJobAsync(jobId, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in BackgroundJobHostedService.");
        }
        _logger.LogInformation("BackgroundJobHostedService stopping.");
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        _logger.LogInformation("Processing job {JobId}", jobId);
        // create scope for DB + handler resolution
        using var scope = _svcProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();

        var job = await db.Set<BackgroundJob>().FirstOrDefaultAsync(j => j.Id == jobId, ct).ConfigureAwait(false);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found in DB.", jobId);
            return;
        }

        if (job.Status != BackgroundJobStatus.Pending)
        {
            _logger.LogInformation("Job {JobId} has status {Status}; skipping.", jobId, job.Status);
            return;
        }

        job.Status = BackgroundJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        job.Progress = 0;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            // resolve handlers within the same scope
            var handlers = scope.ServiceProvider.GetServices<IBackgroundJobHandler>().ToList();
            var handler = handlers.FirstOrDefault(h => string.Equals(h.JobType, job.JobType, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler registered for job type '{job.JobType}'.");
            }

            // Execute handler; handler can update DB progress via RezepteDbContext resolved from scope
            await handler.HandleAsync(job, scope.ServiceProvider, ct).ConfigureAwait(false);

            job.Status = BackgroundJobStatus.Succeeded;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;
            job.ResultMessage ??= "OK";
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Job {JobId} succeeded.", jobId);
        }
        catch (OperationCanceledException)
        {
            job.Status = BackgroundJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            job.Error = "Cancelled";
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Job {JobId} cancelled.", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed.", jobId);
            job.Status = BackgroundJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.Error = ex.Message;
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
