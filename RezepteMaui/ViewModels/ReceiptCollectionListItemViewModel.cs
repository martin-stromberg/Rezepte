using Microsoft.Maui.Graphics;
using Rezepte.Models;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rezepte.ViewModels
{
    public class ReceiptCollectionListItemViewModel : BaseViewModel
    {
        
        public ReceiptCollectionListItemViewModel(ReceiptCollection item, IPictureStorage pictureStorage, INavigationManager navigationManager)
            :base(navigationManager, pictureStorage)
        {
            Item = item;
        }

        public ReceiptCollection Item
        {
            get
            {
                return GetProperty<ReceiptCollection>();
            }
            private set
            {
                SetProperty<ReceiptCollection>(value);
                Name = value?.Name;
                Picture = PictureStorage.Get(value?.PictureHash);
            }
        }

        public bool IsSelected
        {
            get
            {
                return GetProperty<bool>();
            }
            set
            {
                SetProperty<bool>(value);
            }
        }

        public string Name
        {
            get
            {
                return GetProperty<string>();
            }
            set
            {
                SetProperty<string>(value);
            }
        }

        public ImageSource Picture
        {
            get
            {
                return GetProperty<ImageSource>();
            }
            set
            {
                SetProperty<ImageSource>(value);
            }
        }

        public void OpenDetails()
        {
            NavigationManager.OpenReceiptCollection(Item);
        }
    }
}
