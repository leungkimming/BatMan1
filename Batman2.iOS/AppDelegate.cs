using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

using Plugin.DeviceOrientation;

namespace Batman2.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());
            IOSToolbarExtensions.iOS.Renderers.IOSToolbarExtensionsContentPageRenderer
                .SecondaryToolbarUserSettings.Icon = "icons8-more-24.png";
            //IOSToolbarExtensions.iOS.Renderers.IOSToolbarExtensionsContentPageRenderer
            //    .SecondaryToolbarUserSettings.RowHeight = 100;
            return base.FinishedLaunching(app, options);
        }

        [Export("application:supportedInterfaceOrientationsForWindow:")]
        public UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, IntPtr forWindow)
        {
            return DeviceOrientationImplementation.SupportedInterfaceOrientations;
        }

    }
}
