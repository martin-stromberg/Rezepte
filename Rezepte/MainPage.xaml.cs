using Rezepte.ViewModels;

namespace Rezepte
{
    public partial class MainPage: ContentPage
    {

        private int count = 0;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = ViewModel = App.GetService<HomePageViewModel>();
        }

        public HomePageViewModel ViewModel { get; set; }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Task.Run(async () =>
            {
                await Task.Delay(500);                    
                ViewModel?.OnAppeared();
            });
        }

    }

}
