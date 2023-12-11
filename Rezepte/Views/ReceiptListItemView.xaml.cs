
using Rezepte.ViewModels;

namespace Rezepte.Views
{
    public partial class ReceiptListItemView: ContentView
    {

        public ReceiptListItemView()
        {
            InitializeComponent();
        }

        public ReceiptListItemViewModel ViewModel => BindingContext as ReceiptListItemViewModel;

        private void ListItem_Clicked(object sender, TappedEventArgs e)
        {
            ViewModel.OpenDetails();
        }

    }
}