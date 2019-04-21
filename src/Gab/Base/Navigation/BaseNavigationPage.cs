using FormsControls.Base;
using Xamarin.Forms;

namespace Gab.Base.Navigation
{
    public class BaseNavigationPage : NavigationPage
    {
        static readonly IPageAnimation emptyAnimation = new EmptyPageAnimation();
        static readonly IPageAnimation defaultAnimation = new PushPageAnimation();

        public bool EnableInteractivePopGesture { get; set; } = true;

        public bool AnimateNavigationBar { get; set; } = true;

        public BaseNavigationPage()
        {
            this.InitListeners();

            if (Device.RuntimePlatform != Device.Android)
                return;

            BarBackgroundColor = (Color)Application.Current.Resources["ColorPrimary"];
            BarTextColor = Color.White;
        }

        public BaseNavigationPage(Page page)
            : base(page)
        {
            this.InitListeners();

            if (Device.RuntimePlatform != Device.Android)
                return;

            BarBackgroundColor = (Color)Application.Current.Resources["ColorPrimary"];
            BarTextColor = Color.White;
        }

        public static IPageAnimation GetAnimation(Page page, bool animated)
        {
            return !animated ? emptyAnimation : GetPageAnimation(page);
        }

        static IPageAnimation GetPageAnimation(Page page)
        {
            var pageAnimation = (page is IAnimationPage animationPage ? animationPage.PageAnimation : null) ?? defaultAnimation;
            return pageAnimation.Duration <= AnimationDuration.Zero ? emptyAnimation : pageAnimation;
        }

        void InitListeners()
        {
            Popped += OnNavigation;
            PoppedToRoot += OnNavigation;
            Pushed += OnNavigation;
        }

        static void OnNavigation(object sender, NavigationEventArgs e)
        {
            //MessagingCenter.Send(sender, "Iconize.UpdateToolbarItems");
        }
    }
}
