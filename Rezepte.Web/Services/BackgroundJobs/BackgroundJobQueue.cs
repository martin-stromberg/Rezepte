using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;

namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// Represents the background job queue class.
/// </summary>
public class BackgroundJobQueue : IBackgroundJobQueue, IAsyncDisposable
{
    private readonly Channel<Guid> _channel;
    private readonly IServiceProvider _svcProvider;
    private readonly ILogger<BackgroundJobQueue> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundJobQueue"/> class.
    /// </summary>
    /// <param name="svcProvider">The svc provider parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    /// <param name="capacity">The capacity parameter.</param>
    public BackgroundJobQueue(IServiceProvider svcProvider, ILogger<BackgroundJobQueue> logger, int capacity = 100)
    {
        _svcProvider = svcProvider;
        _logger = logger;
        // Bounded channel to exert backpressure
        var opts = new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Guid>(opts);
    }

    /// <summary>
    /// enqueues the async.
    /// </summary>
    /// <param name="jobType">The job type parameter.</param>
    /// <param name="payloadJson">The payload json parameter.</param>
    /// <param name="initiatorUserId">The initiator user id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<Guid> EnqueueAsync(string jobType, string? payloadJson = null, string? initiatorUserId = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // create job in DB (scoped)
        Guid jobId;
        using (var scope = _svcProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
            var job = new BackgroundJob
            {
                JobType = jobType,
                InitiatorUserId = initiatorUserId,
                PayloadJson = payloadJson
            };
            await db.AddAsync(job, ct).ConfigureAwait(false);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            jobId = job.Id;
        }

        // enqueue for processing
        await _channel.Writer.WriteAsync(jobId, ct).ConfigureAwait(false);
        _logger.LogInformation("Enqueued job {JobId} type={JobType} initiator={Initiator}", jobId, jobType, initiatorUserId);
        return jobId;
    }

    /// <summary>
    /// Gets the job async.
    /// </summary>
    /// <param name="jobId">The job id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<BackgroundJob?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        using var scope = _svcProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        return await db.Set<BackgroundJob>().AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct).ConfigureAwait(false);
    }

    // Internal: used by hosted service:
    internal ChannelReader<Guid> Reader => _channel.Reader;

    /// <summary>
    /// disposes the async.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
