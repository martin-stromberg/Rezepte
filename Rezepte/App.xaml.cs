namespace Rezepte
{
    public partial class App: Application
    {

        public App(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            InitializeComponent();

            MainPage = new AppShell();
        }

        public IServiceProvider ServiceProvider { get; }

        public static bool ImportFileGlobal(string path)
        {
            return true;
        }

        public static T GetService<T>()
        {
            return ((App)App.Current).ServiceProvider.GetService<T>();
        }

    }
}
