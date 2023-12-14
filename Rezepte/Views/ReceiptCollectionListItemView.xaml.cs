
using Rezepte.ViewModels;

namespace Rezepte.Views;

public partial class ReceiptCollectionListItemView : ContentView
{
	public ReceiptCollectionListItemView()
	{
		InitializeComponent();
	}
    public ReceiptCollectionListItemViewModel ViewModel => BindingContext as ReceiptCollectionListItemViewModel;
    private void ListItem_Clicked(object sender, TappedEventArgs e)
    {
        ViewModel.OpenDetails();
    }

}