using Rezepte.Services.Navigation;
using System;
using System.Linq;

namespace Rezepte.Services
{
    public static class ServiceExtension
    {

        public static MauiAppBuilder RegisterNavigation(this MauiAppBuilder builder)
        {
            builder.Services.RegisterNavigation();
            return builder;
        }

        public static IServiceCollection RegisterNavigation(this IServiceCollection services)
        {
            services.AddTransient<INavigationManager, NavigationManager>();

            return services;
        }

    }
}
