using Rezepte.Models;
using System;
using System.Linq;

namespace Rezepte.Services.Navigation
{
    public interface INavigationManager
    {
        void NavigateBack();
        void OpenReceiptCard(Receipt item);
        void OpenReceiptCollection(ReceiptCollection item);
    }
}
