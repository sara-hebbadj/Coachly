using Foundation;
using Microsoft.Maui.Authentication;
using UIKit;

namespace Coachly
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (WebAuthenticator.Default.CanHandleCallback(url))
            {
                return WebAuthenticator.Default.HandleCallback(url);
            }

            return base.OpenUrl(app, url, options);
        }
    }
}
