using FormsControls.Base;
using Xamarin.Forms;

namespace Gab.Pages
{
    public partial class MainPage : ContentPage, IAnimationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        #region IAnimationPage

        public void OnAnimationStarted(bool isPopAnimation)
        {
        }

        public void OnAnimationFinished(bool isPopAnimation)
        {
        }

        public IPageAnimation PageAnimation { get; } = new PushPageAnimation
        {
            Subtype = AnimationSubtype.FromRight,
            Duration = AnimationDuration.Medium
        };

        #endregion
    }
}
