using Rezepte.Models;
using Rezepte.ViewModels;

namespace Rezepte.Views;

[QueryProperty(nameof(Collection), "Collection")]
public partial class ReceiptCollectionPage : ContentPage
{
    private ReceiptCollection collection;

    public ReceiptCollectionPage()
	{
		InitializeComponent();
        BindingContext = ViewModel = App.GetService<ReceiptCollectionViewModel>();
    }

	public ReceiptCollection Collection
	{
        get
        {
            return collection;
        }
        set
        {
            collection = value;
            OnPropertyChanged();
        }
    }
    public ReceiptCollectionViewModel ViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.SetParent(Collection);
        ViewModel.OnAppeared();
    }
}