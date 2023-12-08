namespace Rezepte.Tests
{
    public partial class TestPage: ContentPage
    {

        public TestPage()
        {
            InitializeComponent();
            BindingContext = ViewModel = new TestViewModel();
        }

        internal TestViewModel ViewModel { get; }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.RunTestsAsync();
        }

    }

}
