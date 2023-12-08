namespace Rezepte
{
    public partial class App: Application
    {

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        public static bool ImportFileGlobal(string path)
        {
            return true;
        }

    }
}
