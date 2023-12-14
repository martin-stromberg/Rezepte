using Microsoft.Maui.Graphics;
using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.ViewModels
{
    public class ReceiptCollectionViewModel : BaseViewModel
    {
        public ReceiptCollectionViewModel(ReceiptLibrary receiptLibrary, INavigationManager navigationManager, IPictureStorage pictureStorage) : base(navigationManager, pictureStorage)
        {
            this.receiptLibrary = receiptLibrary;
        }

        public void SetParent(ReceiptCollection collection)
        {
            Item = collection;
        }

        public ReceiptCollection Item
        {
            get
            {
                return GetProperty<ReceiptCollection>();
            }
            set
            {
                SetProperty<ReceiptCollection>(value);
                Title = value?.Name;
                LoadItems();
            }
        }
        public ObservableCollection<ReceiptListItemViewModel> Items { get; } = new ObservableCollection<ReceiptListItemViewModel>();

        private long loadItemsSession = 0;
        private readonly ReceiptLibrary receiptLibrary;

        private void LoadItems()
        {
            loadItemsSession = DateTime.Now.Ticks;
            LoadItems(loadItemsSession);
        }

        private void LoadItems(long session, int offset = 0, int count = 10)
        {
            if (loadItemsSession != session)
                return;
            var items = receiptLibrary.GetRange(Item, offset, count);
            foreach (var item in items)
                if (loadItemsSession == session)
                {
                    if (offset == 0)
                        Items.Clear();
                    Add(item);
                    offset++;
                }
            if (!items.Any())
                return;
            LoadItems(session, offset, count);
        }

        private void Add(Receipt item)
        {
            if (!Items.Any(vm => vm.Item.Id == item.Id))
                Items.Insert(0, new ReceiptListItemViewModel(item, PictureStorage, NavigationManager));
        }
    }
}
