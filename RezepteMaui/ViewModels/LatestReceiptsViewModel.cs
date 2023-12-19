using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Services.History;
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
    public class LatestReceiptsViewModel : BaseViewModel
    {
        private readonly ReceiptHistory receiptHistory;

        public LatestReceiptsViewModel(ReceiptHistory receiptHistory, INavigationManager navigationManager, IPictureStorage pictureStorage) 
            : base(navigationManager, pictureStorage)
        {
            this.receiptHistory = receiptHistory;
            this.receiptHistory.ReceiptRemoved += ReceiptLibrary_ReceiptRemoved;
            this.receiptHistory.ReceiptAdded += ReceiptLibrary_ReceiptAdded;            
            AddEmptyReceipt();
        }

        private async void AddEmptyReceipt()
        {
            await Add(new Receipt() { Title = string.Empty });
            isFirst = true;
        }

        private async void ReceiptLibrary_ReceiptAdded(object sender, BaseModelEventArgs e)
        {
            await Add(e.Item as Receipt);
        }

        private async void ReceiptLibrary_ReceiptRemoved(object sender, BaseModelEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var existing = Items.FirstOrDefault(c => c.Item.Id == e.Item.Id);
                if (existing != null)
                    Items.Remove(existing);
            });
        }

        public override void OnAppeared()
        {
            base.OnAppeared();
            receiptHistory.Initialize();
        }
        private bool isFirst = false;
        private async Task Add(Receipt item)
        {            
            if (!Items.Any(vm => vm.Item.Id == item.Id))
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    if (!Items.Any())
                        Items.Add(new ReceiptListItemViewModel(item, PictureStorage, NavigationManager));
                    else
                    { 
                        Items.Insert(0, new ReceiptListItemViewModel(item, PictureStorage, NavigationManager));
                        if (isFirst)
                            Items.RemoveAt(Items.Count - 1);
                    }
                    isFirst = false;
                });
        }

        public ObservableCollection<ReceiptListItemViewModel> Items { get; } = new ObservableCollection<ReceiptListItemViewModel>();
    }
}
