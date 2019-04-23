using System;
using System.Threading.Tasks;
using Foundation;
using ImageCircle.Forms.Plugin.iOS;
using Syncfusion.ListView.XForms.iOS;
using Syncfusion.SfPullToRefresh.XForms.iOS;
using UIKit;
using Xamarin.Forms;

namespace Gab.iOS
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
            #region Error Handling

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            #endregion

            Forms.Init();
            ImageCircleRenderer.Init();
            FormsControls.Touch.Main.Init();
            SfPullToRefreshRenderer.Init();
            SfListViewRenderer.Init();
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }

        #region Error Handling

        static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            var ex = new Exception("TaskSchedulerOnUnobservedTaskException", unobservedTaskExceptionEventArgs.Exception);
        }

        static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var ex = new Exception("CurrentDomainOnUnhandledException", unhandledExceptionEventArgs.ExceptionObject as Exception);
        }

        #endregion
    }
}
