using Microsoft.Maui.Graphics;
using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.ViewModels
{
    public class ReceiptCollectionViewModel : BaseViewModel
    {
        public ReceiptCollectionViewModel(ReceiptLibrary receiptLibrary, INavigationManager navigationManager, IPictureStorage pictureStorage) : base(navigationManager, pictureStorage)
        {
            this.receiptLibrary = receiptLibrary;
            MenuAction = new Command((args) => ExecuteMenuAction(args as string));
            IsGeneralMode = true;
        }

        private void ExecuteMenuAction(string arg)
        {
            switch (arg)
            {
                case "addReceipt":
                    ExecuteAddReceipt();
                    break;
                case "addURI":
                    ExecuteAddURI();
                    break;
            }
        }

        private async void ExecuteAddURI()
        {
            IsAddingMode = false;
            try
            {
                var uris = NewReceiptUri.Split("\r");
                foreach (var uri in uris)
                    try
                    {
                        var receiptUri = uri.Trim();
                        if (string.IsNullOrWhiteSpace(receiptUri))
                            continue;
                        var existingReceipt = receiptLibrary.FindReceiptByUri(receiptUri);
                        if (existingReceipt != null)
                        {
                            if (!receiptLibrary.IsInCollection(existingReceipt, Item))
                            {
                                receiptLibrary.AddToCollection(existingReceipt, Item);
                                Add(existingReceipt);
                            }
                            continue;
                        }
                            var receipts = await receiptLibrary.CreateReceipts(receiptUri);
                            if (receipts == null)
                                throw new ApplicationException($"Für die angegebene Adresse konnte kein Rezept ermittelt werden.");
                        foreach (var receipt in receipts)
                        {
                            existingReceipt = receiptLibrary.FindReceiptByUri(receipt.Uri);
                            if (existingReceipt != null)
                            {
                                if (!receiptLibrary.IsInCollection(existingReceipt, Item))
                                {
                                    receiptLibrary.AddToCollection(existingReceipt, Item);
                                    Add(existingReceipt);
                                }                                
                                continue;
                            }

                            receiptLibrary.Add(receipt);
                            receiptLibrary.AddToCollection(existingReceipt, Item);
                            Add(receipt);
                        }
                        
                    }
                    catch { }
                IsGeneralMode = true;
            }
            catch
            {
                IsAddingMode = true;
            }
        }
        public string NewReceiptUri
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
        private void ExecuteAddReceipt()
        {
            IsAddingMode = true;
        }
        public bool IsAddingMode
        {
            get
            {
                return GetProperty<bool>();
            }
            set
            {
                SetProperty<bool>(value);
                if (value)
                    IsGeneralMode = false;
            }
        }
        public bool IsGeneralMode
        {
            get
            {
                return GetProperty<bool>();
            }
            set
            {
                SetProperty<bool>(value);
                if (value)
                    IsAddingMode = false;
            }
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

        public Command MenuAction { get; }

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
