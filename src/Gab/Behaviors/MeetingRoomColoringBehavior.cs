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
        WeakReference<View> view;
        IDisposable subscription;

        public static BindableProperty ObservableCollectionProperty =
            BindableProperty.Create(nameof(ObservableCollection), typeof(IList<MeetingRoom>), typeof(MeetingRoomColoringBehavior), propertyChanged: OnCollectionChanged);
        public IList<MeetingRoom> ObservableCollection
        {
            get => (IList<MeetingRoom>)GetValue(ObservableCollectionProperty);
            set
            {
                SetValue(ObservableCollectionProperty, value);
                CalculateColor();
            }
        }

        public static BindableProperty IndexProperty = BindableProperty.Create(nameof(Index), typeof(int), typeof(MeetingRoomColoringBehavior), default(int));
        public int Index
        {
            get => (int)GetValue(IndexProperty);
            set => SetValue(IndexProperty, value);
        }

        public static BindableProperty CountProperty = BindableProperty.Create(nameof(Count), typeof(int), typeof(MeetingRoomColoringBehavior), default(int));
        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        static void OnCollectionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (MeetingRoomColoringBehavior)bindable;
            self.subscription?.Dispose();

            var newCollection = newValue as ObservableCollection<MeetingRoom>;
            self.subscription = newCollection?.WhenCollectionChanged().Subscribe(x => self.CalculateColor());
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
            subscription?.Dispose();
        }

        async void CalculateColor()
        {
            //yield control to avoid a race condition
            await Task.Delay(1);
            View sameView = null;
            if (ObservableCollection == null || view?.TryGetTarget(out sameView) != true || !(sameView.BindingContext is MeetingRoom item))
                return;
            try
            {
                var index = ObservableCollection.IndexOf(item);
                var count = ObservableCollection.Count;
                var colors = Constants.Colors.MeetingRoomColors;

                var color = colors[(count - index - 1) % colors.Count];
                item.Color = color.ToHex();
                sameView.BackgroundColor = color;
            }
            catch(Exception ex)
            {
                // Let's not crash because of a coloring fail :)
                Console.WriteLine(ex.Message);
            }
        }
    }
}
