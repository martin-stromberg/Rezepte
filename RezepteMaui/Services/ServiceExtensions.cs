using Rezepte.Extensions;
using Rezepte.Services.AppToApp;
using Rezepte.Services.Chefkoch;
using Rezepte.Services.Database;
using Rezepte.Services.KabelEins;
using Rezepte.Services.PictureStorage;
using System;
using System.Linq;

namespace Rezepte.Services
{
    public static class ViewModelExtensions
    {

        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            builder.Services.RegisterServices();
            return builder;
        }

        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection)
        {
            var rootPath = FileSystem.Current.AppDataDirectory;            
            serviceCollection.AddTransient<CockingDatabaseSettings>(sp =>
            {
                var databasePath = Path.Combine(rootPath, "Database");
                if (!Directory.Exists(databasePath))
                    Directory.CreateDirectory(databasePath);

                // string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return new CockingDatabaseSettings() { FilePath = Path.Combine(databasePath, "Rezepte.db3") };
            });
            serviceCollection.AddSingleton<ReceiptLibrary>(sp =>
                                                           new ReceiptLibrary(sp.GetService<ICockingDatabase>(),
                                                                              sp.GetService<IPictureStorage>(),
                                                                              new IReceiptSource[]
                {
                    sp.GetService<ChefkochSite>(),
                    sp.GetService<KabelEinsRezeptSammlung>()
                }));
            serviceCollection.AddTransient<ChefkochSite>();
            serviceCollection.AddTransient<KabelEinsRezeptSammlung>();
            serviceCollection.AddSingleton<ICockingDatabase, CockingDatabase>();
            serviceCollection.AddTransient<IPictureStorageSettings>(sp =>
            {
                var picturePath = Path.Combine(rootPath, "Pictures");
                if (!Directory.Exists(picturePath))
                    Directory.CreateDirectory(picturePath);
                return new PictureStorageSettings() { RootPath = picturePath };
            });
            serviceCollection.AddTransient<IPictureStorage, PictureStorage.PictureStorage>();
            serviceCollection.AddSingleton<SyncManager>();
            serviceCollection.AddSingleton<SyncManagerSettings>(sp =>
            {
                return SyncManagerSettings.LoadAsync().Wait<SyncManagerSettings>();
            });
            return serviceCollection;
        }

    }
}
