using Rezepte.Models;
using System;
using System.Linq;

namespace Rezepte.Services.Navigation
{
    public interface INavigationManager
    {
        void OpenReceiptCard(Receipt item);
    }
}
