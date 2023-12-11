using Rezepte.Models;
using Rezepte.Services;
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

        public HomePageViewModel(
            ReceiptLibrary receiptLibrary,
            IPictureStorage pictureStorage,
            INavigationManager navigationManager)
            : base(navigationManager, pictureStorage)
        {
            _ReceiptLibrary = receiptLibrary;
            MenuAction = new Command((args) => ExecuteMenuAction(args as string));
            IsGeneralMode = true;
            NewReceiptUri = "https://www.chefkoch.de/rezepte/926481197910350/Schokoladenbrot.html";
        }

        public override void OnAppeared()
        {
            base.OnAppeared();
            LoadItems();
        }

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
                    IsAddingMode = false;
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
            }
        }

        private void ExecuteAddReceipt()
        {
            IsAddingMode = true;
        }

        private async void ExecuteAddURI()
        {
            IsAddingMode = false;
            try
            {
                var receipt = await _ReceiptLibrary.CreateFromUri(NewReceiptUri);
                if (receipt == null)
                    throw new ApplicationException($"Für die angegebene Adresse konnte kein Rezept ermittelt werden.");

                _ReceiptLibrary.Add(receipt);
                Add(receipt);
                IsGeneralMode = true;
            }
            catch
            {
                IsAddingMode = true;
            }
        }

        private int currentOffset = 0;
        private int currentCount = 10;

        private void LoadItems()
        {
            var items = _ReceiptLibrary.GetRange(currentOffset, currentCount);
            foreach (var item in items)
            {
                Add(item);
                currentOffset++;
            }
            if (!items.Any())
                return;
            LoadItems();
        }

        private void Add(Receipt item)
        {
            Items.Add(new ReceiptListItemViewModel(item, PictureStorage, NavigationManager));
        }

        public ObservableCollection<ReceiptListItemViewModel> Items { get; } = new ObservableCollection<ReceiptListItemViewModel>();

    }
}
