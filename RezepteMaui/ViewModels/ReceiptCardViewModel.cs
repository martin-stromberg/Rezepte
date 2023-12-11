using Rezepte.Models;
using Rezepte.Services.Navigation;
using Rezepte.Services.PictureStorage;
using System;
using System.Linq;

namespace Rezepte.ViewModels
{
    public class ReceiptCardViewModel: BaseViewModel
    {

        public ReceiptCardViewModel(INavigationManager navigationManager, IPictureStorage pictureStorage)
            : base(navigationManager, pictureStorage) { }

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

    }
}
