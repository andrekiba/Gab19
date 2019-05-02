using System;
using System.ComponentModel;
using Android.Content;
using Android.Content.Res;
using Plugin.Iconize;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;
using Orientation = Android.Widget.Orientation;

namespace Gab.Droid.Renderers
{
    public class BaseNavigationPageRenderer : NavigationPageRenderer
    {
        Orientation orientation = Orientation.Vertical;

        public BaseNavigationPageRenderer(Context context)
            : base(context)
        {
            // Intentionally left blank
        }

        protected override void OnElementChanged(ElementChangedEventArgs<NavigationPage> e)
        {
            base.OnElementChanged(e);
            HandleProperties();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            HandleProperties();
        }

        protected override void OnAttachedToWindow()
        {
            MessagingCenter.Subscribe<object>(this, IconToolbarItem.UpdateToolbarItemsMessage, OnUpdateToolbarItems);

            HandleProperties();
            base.OnAttachedToWindow();
        }

        //protected override void OnLayout(bool changed, int l, int t, int r, int b)
        //{
        //    base.OnLayout(changed, l, t, r, b);
        //    Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        //    {
        //        OnUpdateToolbarItems(this);
        //        return false;
        //    });
        //}

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            var newOrientation = newConfig.Orientation;
            if (newOrientation == Android.Content.Res.Orientation.Portrait && orientation == Orientation.Vertical ||
                newOrientation == Android.Content.Res.Orientation.Landscape && orientation != Orientation.Horizontal)
                return;

            orientation = orientation == Orientation.Vertical ? Orientation.Horizontal : Orientation.Vertical;
            Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                OnUpdateToolbarItems(this);
                return false;
            });
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            var toolbarItems = Element.GetToolbarItems();
            if (toolbarItems != null)
            {
                foreach (var item in toolbarItems)
                {
                    item.PropertyChanged -= HandleToolbarItemPropertyChanged;
                }
            }
            MessagingCenter.Unsubscribe<object>(this, IconToolbarItem.UpdateToolbarItemsMessage);
        }

        void HandleProperties()
        {
            var toolbarItems = Element.GetToolbarItems();
            if (toolbarItems != null)
            {
                foreach (var item in toolbarItems)
                {
                    item.PropertyChanged -= HandleToolbarItemPropertyChanged;
                    item.PropertyChanged += HandleToolbarItemPropertyChanged;
                }
            }
            OnUpdateToolbarItems(this);
        }

        void OnUpdateToolbarItems(object sender)
        {
            //Element?.UpdateToolbarItems(this);
        }

        void HandleToolbarItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MenuItem.IsEnabled)
                || e.PropertyName == nameof(MenuItem.Text)
                || e.PropertyName == nameof(MenuItem.Icon))
            {
                Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
                {
                    OnUpdateToolbarItems(this);
                    return false;
                });
            }
        }
    }
}