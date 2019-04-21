using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gab.Base;
using Gab.Shared.Models;
using Xamarin.Forms;

namespace Gab.Behaviors
{
    public class MeetingRoomColoringBehavior : Behavior<View>
    {
        private WeakReference<View> view;

        public static BindableProperty MeetingRoomCollectionProperty =
            BindableProperty.Create(nameof(MeetingRoomCollection), typeof(IList<MeetingRoom>), typeof(MeetingRoomColoringBehavior), propertyChanged: OnCollectionChanged);

        public IList<MeetingRoom> MeetingRoomCollection
        {
            get => (IList<MeetingRoom>)GetValue(MeetingRoomCollectionProperty);
            set
            {
                SetValue(MeetingRoomCollectionProperty, value);
                CalculateColor();
            }
        }

        public static BindableProperty MeetingRoomIndexProperty = BindableProperty.Create(nameof(MeetingRoomIndex), typeof(int), typeof(MeetingRoomColoringBehavior), default(int));

        public int MeetingRoomIndex
        {
            get => (int)GetValue(MeetingRoomIndexProperty);
            set => SetValue(MeetingRoomIndexProperty, value);
        }

        public static BindableProperty MeetingRoomCountProperty = BindableProperty.Create(nameof(MeetingRoomCount), typeof(int), typeof(MeetingRoomColoringBehavior), default(int));

        public int MeetingRoomCount
        {
            get => (int)GetValue(MeetingRoomCountProperty);
            set => SetValue(MeetingRoomCountProperty, value);
        }

        private static void OnCollectionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (MeetingRoomColoringBehavior)bindable;

            var newCollection = newValue as ObservableCollection<MeetingRoom>;
            newCollection?.WhenCollectionChanged(
                collection =>
                {
                    self.CalculateColor();
                });
        }

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            view = new WeakReference<View>(bindable);
            CalculateColor();
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);
            view = null;
        }

        async void CalculateColor()
        {
            //yield control to avoid a race condition
            await Task.Delay(1);
            View sameView = null;
            MeetingRoom item;
            if (MeetingRoomCollection == null || view?.TryGetTarget(out sameView) != true || (item = sameView.BindingContext as MeetingRoom) == null)
                return;
            try
            {
                var index = MeetingRoomCollection.IndexOf(item);
                var count = MeetingRoomCollection.Count;
                var colors = Constants.Colors.MeetingRoomColors;

                var backgroundColor = colors[(count - index - 1) % colors.Count];

                sameView.BackgroundColor = backgroundColor;
            }
            catch
            {
                // Let's not crash because of a coloring fail :)
            }
        }
    }
}
