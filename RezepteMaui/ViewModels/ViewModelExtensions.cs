using System;
using System.Linq;

namespace Rezepte.ViewModels
{
    public static class ViewModelExtensions
    {

        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
        {
            builder.Services.RegisterViewModels();
            return builder;
        }

        public static IServiceCollection RegisterViewModels(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<HomePageViewModel>();
            serviceCollection.AddTransient<ReceiptCardViewModel>();
            serviceCollection.AddTransient<ReceiptListItemViewModel>();
            serviceCollection.AddTransient<ReceiptCollectionViewModel>();
            serviceCollection.AddTransient<LatestReceiptsViewModel>();
            return serviceCollection;
        }

    }
}
