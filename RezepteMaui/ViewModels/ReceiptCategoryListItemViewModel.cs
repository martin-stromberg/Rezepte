using Rezepte.Models;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.ViewModels
{
    public class ReceiptCategoryListItemViewModel : BaseViewModel
    {
        public ReceiptCategoryListItemViewModel(ReceiptCollection item, IPictureStorage pictureStorage, INavigationManager navigationManager)
            :base(navigationManager, pictureStorage)
        {
            Item = item;
        }

        public ReceiptCollection Item { get; }
    }
}
