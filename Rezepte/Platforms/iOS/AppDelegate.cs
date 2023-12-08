using Foundation;
using UIKit;

namespace Rezepte.Platforms.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate: MauiUIApplicationDelegate
    {

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        {
            return App.ImportFileGlobal(url.Path);
        }

    }

}
