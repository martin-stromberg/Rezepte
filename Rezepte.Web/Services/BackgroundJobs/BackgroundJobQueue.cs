using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;

namespace Rezepte.Web.Services.BackgroundJobs;

public class BackgroundJobQueue : IBackgroundJobQueue, IAsyncDisposable
{
    private readonly Channel<Guid> _channel;
    private readonly IServiceProvider _svcProvider;
    private readonly ILogger<BackgroundJobQueue> _logger;

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

    public async Task<BackgroundJob?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        using var scope = _svcProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        return await db.Set<BackgroundJob>().AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct).ConfigureAwait(false);
    }

    // Internal: used by hosted service:
    internal ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}