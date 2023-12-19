using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Services.AppToApp;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rezepte.ViewModels
{
    public class HomePageViewModel: BaseViewModel
    {

        private readonly ReceiptLibrary _ReceiptLibrary;
        private readonly SyncManager syncManager;

        public HomePageViewModel(
            LatestReceiptsViewModel latestReceiptsViewModel,
            ReceiptLibrary receiptLibrary,
            SyncManager syncManager,
            IPictureStorage pictureStorage,
            INavigationManager navigationManager)
            : base(navigationManager, pictureStorage)
        {
            _ReceiptLibrary = receiptLibrary;
            this.syncManager = syncManager;
            _ReceiptLibrary.ReceiptCollectionRemoved += _ReceiptLibrary_ReceiptCollectionRemoved;

            MenuAction = new Command((args) => ExecuteMenuAction(args as string));
            IsGeneralMode = true;
            NewReceiptUri = string.Empty;
            LatestReceipts = latestReceiptsViewModel;
        }


        private void _ReceiptLibrary_ReceiptCollectionRemoved(object sender, BaseModelEventArgs e)
        {
            var existing = Collections.FirstOrDefault(c => c.Item.Id == e.Item.Id);
            if (existing != null)
                Collections.Remove(existing);
        }
        private bool isFirst = true;
        public override async void OnAppeared()
        {
            base.OnAppeared();
            await LoadCollections().ConfigureAwait(false);
            LatestReceipts?.OnAppeared();
            if (isFirst)
                await Task.Run(async () => { 
                    await Task.Delay(2000).ConfigureAwait(false);
                    syncManager.Sync();
                }).ConfigureAwait(false);
            isFirst = false;            
        }

        public LatestReceiptsViewModel LatestReceipts { get; }

        public Command MenuAction { get; }

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
                {
                    IsAddingMode = false;
                    IsAddingCollectionMode = false;
                }
            }
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
        public bool IsAddingCollectionMode
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
        public string NewCollectionName
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

        private void ExecuteMenuAction(string args)
        {
            switch (args)
            {
                case "addReceipt":
                    ExecuteAddReceipt();
                    break;
                case "addURI":
                    ExecuteAddURI();
                    break;
                case "cancelURI":
                    ExecuteCancelURI();
                    break;

                case "addCollection":
                    ExecuteAddCollection();
                    break;
                case "cancelCollectionName":
                    ExecuteCancelCollectionName();
                    break;
                case "addCollectionName":
                    ExecuteAddCollectionNameAsync();
                    break;

                case "sync":
                    ExecuteSync();
                    break;
            }
        }

        private void ExecuteSync()
        {
            Task.Run(() =>
            {
                syncManager.Sync(false);
            });
        }

        private async void ExecuteAddCollectionNameAsync()
        {
            IsAddingCollectionMode = false;
            try
            {
                var receiptCollection = await _ReceiptLibrary.CreateCollectionFromName(NewCollectionName);
                if (receiptCollection == null)
                    throw new ApplicationException($"Für die angegebene Adresse konnte kein Rezept ermittelt werden.");

                _ReceiptLibrary.Add(receiptCollection);
                await Add(receiptCollection);
                IsGeneralMode = true;
            }
            catch(Exception ex)
            {
                NewCollectionName = ex.Message;
                IsAddingCollectionMode = true;
            }
        }

        private void ExecuteCancelCollectionName()
        {
            IsGeneralMode = true;
        }

        private void ExecuteAddCollection()
        {
            NewCollectionName = string.Empty;
            IsAddingCollectionMode = true;
        }

        private void ExecuteAddReceipt()
        {
            NewReceiptUri = string.Empty;
            IsAddingMode = true;
        }
        private void ExecuteCancelURI()
        {
            IsGeneralMode = true;
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
                        var existingReceipt = _ReceiptLibrary.FindReceiptByUri(receiptUri);
                        if (existingReceipt != null) continue;

                        var receipts = await _ReceiptLibrary.CreateReceipts(receiptUri);
                        if (receipts == null)
                            throw new ApplicationException($"Für die angegebene Adresse konnte kein Rezept ermittelt werden.");
                        foreach (var receipt in receipts)
                        {
                            existingReceipt = _ReceiptLibrary.FindReceiptByUri(receipt.Uri);
                            if (existingReceipt != null) continue;

                            _ReceiptLibrary.Add(receipt);
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

        public ObservableCollection<ReceiptCollectionListItemViewModel> Collections { get; } = new ObservableCollection<ReceiptCollectionListItemViewModel>();

        private async Task LoadCollections()
        {
            var categories = _ReceiptLibrary.GetCollections();
            foreach (var category in categories)
                await Add(category);
        }
        private async Task Add(ReceiptCollection item)
        {
            if (!Collections.Any(vm => vm.Item.Id == item.Id))
                await MainThread.InvokeOnMainThreadAsync(() => { Collections.Add(new ReceiptCollectionListItemViewModel(item, PictureStorage, NavigationManager)); });
        }
    }
}
