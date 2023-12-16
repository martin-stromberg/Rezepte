using Rezepte.Models;
using Rezepte.Views;
using System;
using System.Linq;

namespace Rezepte.Services.Navigation
{
    public class NavigationManager: INavigationManager
    {

        public NavigationManager()
        {
            Routing.RegisterRoute("receipt", typeof(ReceiptCardPage));
            Routing.RegisterRoute("receiptCollection", typeof(ReceiptCollectionPage));
        }

        protected void NavigateToRoute(string route, Dictionary<string, object> args = null)
        {
            if (args == null)
                Shell.Current.GoToAsync(route);
            else
                Shell.Current.GoToAsync(route, args);
        }

        public void OpenReceiptCard(Receipt item)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "Receipt", item }
            };
            NavigateToRoute("receipt", navigationParameter);
        }

        public void OpenReceiptCollection(ReceiptCollection item)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "Collection", item }
            };
            NavigateToRoute("receiptCollection", navigationParameter);
        }

        public void NavigateBack()
        {
            MainThread.InvokeOnMainThreadAsync(() => { Shell.Current.Navigation.RemovePage(Shell.Current.CurrentPage); });
        }
    }
}
