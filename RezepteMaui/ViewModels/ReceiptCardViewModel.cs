using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;

namespace Rezepte.ViewModels
{
    public class ReceiptCardViewModel: BaseViewModel
    {
        private readonly ReceiptLibrary _ReceiptLibrary;
        public ReceiptCardViewModel(
            ReceiptLibrary receiptLibrary,
            INavigationManager navigationManager, IPictureStorage pictureStorage)
            : base(navigationManager, pictureStorage) {
            _ReceiptLibrary = receiptLibrary;
            MenuAction = new Command((args) => ExecuteMenuAction(args as string));
            IsGeneralMode = true;
        }

        private void ExecuteMenuAction(string args)
        {
            switch (args)
            {
                case "addCollection":
                    ExecuteAddCollection();
                    break;
                case "saveCollections":
                    ExecuteSaveCollections();
                    break;
            }
        }

        public override void OnAppeared()
        {
            base.OnAppeared();
            LoadCollections();
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
                {
                    IsEditCollectionsMode = false;
                }
            }
        }
        public bool IsEditCollectionsMode
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
                    IsGeneralMode = false;
                }
            }
        }

        private void ExecuteAddCollection()
        {
            IsEditCollectionsMode = true;
        }
        private void ExecuteSaveCollections()
        {
            foreach (var collection in Collections)
            {
                var receiptCollection = collection.Item;
                var ReceiptIsInCollection = _ReceiptLibrary.IsInCollection(Item, receiptCollection);
                if (collection.IsSelected && !ReceiptIsInCollection)
                    _ReceiptLibrary.AddToCollection(Item, receiptCollection);
                else if (!collection.IsSelected && ReceiptIsInCollection)
                    _ReceiptLibrary.RemoveFromCollection(Item, receiptCollection);
            }

            IsGeneralMode = true;
        }

        public void SetParent(Receipt item)
        {
            Item = item;
        }

        public Receipt Item
        {
            get
            {
                return GetProperty<Receipt>();
            }
            set
            {
                SetProperty<Receipt>(value);
                Title = value?.Title;
                Instructions = value?.Instructions;
                Picture = PictureStorage.Get(value?.PictureHashes?.FirstOrDefault());
                Ingredients = value?.Ingredients?.Clone() as ReceiptIngredients;
            }
        }
        public ObservableCollection<ReceiptCollectionListItemViewModel> Collections { get; } = new ObservableCollection<ReceiptCollectionListItemViewModel>();
        private void LoadCollections()
        {
            var categories = _ReceiptLibrary.GetCollections();
            foreach (var category in categories)
                Add(category);
        }
        private void Add(ReceiptCollection item)
        {
            Collections.Insert(0, 
                new ReceiptCollectionListItemViewModel(item, PictureStorage, NavigationManager)
                {
                    IsSelected = _ReceiptLibrary.IsInCollection(Item, item)
                });
        }

        public string Instructions
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

        public ReceiptIngredients Ingredients
        {
            get
            {
                return GetProperty<ReceiptIngredients>();
            }
            set
            {
                SetProperty<ReceiptIngredients>(value);
            }
        }

        public Command MenuAction { get; }
    }
}
