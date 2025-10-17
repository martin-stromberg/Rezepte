using Microsoft.Extensions.DependencyInjection;
using Rezepte.Web.Services.BackgroundJobs;

namespace Rezepte.Web.Extensions;

public static class JobQueueServiceCollectionExtensions
{
    /// <summary>
    /// Registers job queue, hosted worker and job handler discovery.
    /// Register your IBackgroundJobHandler implementations separately (Scoped/Transient).
    /// </summary>
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
