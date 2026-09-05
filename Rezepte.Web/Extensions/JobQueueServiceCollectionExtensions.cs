using Microsoft.Extensions.DependencyInjection;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Extensions;

/// <summary>
/// Represents the job queue service collection extensions class.
/// </summary>
public static class JobQueueServiceCollectionExtensions
{
    /// <summary>
    /// Registers job queue, hosted worker and job handler discovery.
    /// Register your IBackgroundJobHandler implementations separately (Scoped/Transient).
    /// </summary>
    /// <param name="services">The services parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    public static IServiceCollection AddBackgroundJobQueue(this IServiceCollection services)
    {
        // queue as singleton
        services.AddSingleton<BackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobQueue>(sp => sp.GetRequiredService<BackgroundJobQueue>());

        // hosted worker
        services.AddHostedService<BackgroundJobHostedService>(sp => new BackgroundJobHostedService(
            sp.GetRequiredService<BackgroundJobQueue>(),
            sp,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BackgroundJobHostedService>>()
        ));

        return services;
    }
}
