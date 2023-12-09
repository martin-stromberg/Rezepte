using Microsoft.Extensions.Logging;
using Rezepte.Services;
using Rezepte.Tests;
using System.Reflection;

namespace Rezepte
{
    public static class MauiProgram
    {

        public static MauiApp CreateMauiApp()
        {
            var assemblyConfigurationAttribute = typeof(MauiProgram).Assembly
                                                                    .GetCustomAttribute<AssemblyConfigurationAttribute>();
            var buildConfigurationName = assemblyConfigurationAttribute?.Configuration;

            if (buildConfigurationName == "Tests")
                return CreateTestApp();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .RegisterServices();

            #if DEBUG
            builder.Logging.AddDebug();
            #endif

            return builder.Build();
        }

        private static MauiApp CreateTestApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<TestApp>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            #if DEBUG
            builder.Logging.AddDebug();
            #endif

            return builder.Build();
        }

    }
}
