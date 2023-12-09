using Rezepte.Services.Chefkoch;
using Rezepte.Services.Database;
using Rezepte.ViewModels;
using System;
using System.Linq;

namespace Rezepte.Services
{
    public static class ServiceExtensions
    {

        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            builder.Services.RegisterServices();
            return builder;
        }

        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<CockingDatabaseSettings>(sp =>
            {
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                folder = Path.Combine(folder, "Rezepte");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                return new CockingDatabaseSettings() { FilePath = Path.Combine(folder, "Rezepte.db3") };
            });
            serviceCollection.AddTransient<ReceiptLibrary>(sp =>
                                                           new ReceiptLibrary(sp.GetService<ICockingDatabase>(),
                new IReceiptSource[]
                {
                    sp.GetService<ChefkochSite>()
                }));
            serviceCollection.AddTransient<ChefkochSite>();
            serviceCollection.AddSingleton<ICockingDatabase, CockingDatabase>();
            serviceCollection.AddTransient<HomePageViewModel>();
            return serviceCollection;
        }

    }
}
