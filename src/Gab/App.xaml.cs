using FreshMvvm;
using Gab.Base.Navigation;
using Gab.Services;
using Gab.ViewModels;
using Plugin.Iconize;
using Plugin.Iconize.Fonts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Constants = Gab.Base.Constants;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Gab
{
    public partial class App : Application
    {
        #region Properties
        public static App Instance { get; private set; }

        #endregion

        public App()
        {
            InitializeComponent();

            Instance = this;

            SetupIoc();

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SyncfusionLicenseKey);
            Iconize.With(new FontAwesomeRegularModule()).With(new FontAwesomeSolidModule());

            SetStartPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        #region Methods

        public void SetStartPage()
        {
            var mainPagePage = FreshPageModelResolver.ResolvePageModel<MeetingRoomsViewModel>();
            var mainContainer = new FreshIconAnimationNavigationContainer(mainPagePage);

            MainPage = mainContainer;
        }

        static void SetupIoc()
        {
            FreshIOC.Container.Register<IMeetingRoomsService, MeetingRoomsService>();

            FreshTinyIOCBuiltIn.Current.Register<MeetingRoomsViewModel>();
            FreshTinyIOCBuiltIn.Current.Register<MeetingRoomViewModel>();
        }

        #endregion 
    }
}
