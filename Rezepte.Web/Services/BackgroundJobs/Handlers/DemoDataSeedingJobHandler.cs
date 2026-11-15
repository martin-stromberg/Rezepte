namespace Rezepte.Web.Services.BackgroundJobs.Handlers;

/// <summary>
/// Triggers the demo data seeding for a newly registered user via <see cref="IDemoDataSeeder"/>.
/// </summary>
public class DemoDataSeedingJobHandler : IBackgroundJobHandler
{
    /// <summary>
    /// The job type name under which this handler is registered and enqueued.
    /// </summary>
    public const string JobTypeName = "seed-demo-data";

    /// <inheritdoc />
    public string JobType => JobTypeName;

    /// <inheritdoc />
    public async Task HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct)
    {
        if (job is null) throw new ArgumentNullException(nameof(job));
        ct.ThrowIfCancellationRequested();

        var payload = DemoDataSeedPayload.FromJson(job.PayloadJson);
        if (string.IsNullOrWhiteSpace(payload.UserId))
        {
            throw new InvalidOperationException("Demo data seeding job has no user id.");
        }

        var userService = scopeServices.GetRequiredService<IUserService>();
        var user = await userService.GetByIdAsync(payload.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{payload.UserId}' does not exist.");
        }

        var demoDataSeeder = scopeServices.GetRequiredService<IDemoDataSeeder>();
        await demoDataSeeder.SeedForUserAsync(payload.UserId, ct).ConfigureAwait(false);
    }
}
