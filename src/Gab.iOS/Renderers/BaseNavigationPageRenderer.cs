using Plugin.Iconize;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace Gab.iOS.Renderers
{
    public class BaseNavigationPageRenderer : NavigationRenderer
    {
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            MessagingCenter.Subscribe<object>(this, IconToolbarItem.UpdateToolbarItemsMessage, OnUpdateToolbarItems);
            OnUpdateToolbarItems(this);
        }

        public override void ViewWillDisappear(bool animated)
        {
            MessagingCenter.Unsubscribe<object>(this, IconToolbarItem.UpdateToolbarItemsMessage);

            base.ViewWillDisappear(animated);
        }

        void OnUpdateToolbarItems(object sender)
        {
            (Element as NavigationPage)?.UpdateToolbarItems(this);
        }
    }
}