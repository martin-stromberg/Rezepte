using Rezepte.Models;
using Rezepte.ViewModels;

namespace Rezepte.Views
{
    [QueryProperty(nameof(Item), "Receipt")]
    public partial class ReceiptCardPage: ContentPage
    {

        public ReceiptCardPage()
        {
            InitializeComponent();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior()
            {
                TextOverride = "⏪ ",
            });
            BindingContext = ViewModel = App.GetService<ReceiptCardViewModel>();
        }

        private Receipt item;

        public Receipt Item
        {
            get
            {
                return item;
            }
            set
            {
                item = value;
                OnPropertyChanged();
            }
        }

        public ReceiptCardViewModel ViewModel { get; }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.SetParent(Item);
            ViewModel.OnAppeared();
        }

    }
}