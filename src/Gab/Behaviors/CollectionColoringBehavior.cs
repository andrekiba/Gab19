using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gab.Base;
using Xamarin.Forms;

namespace Gab.Behaviors
{
    public class CollectionColoringBehavior<T> : Behavior<View> where T : class
    {
        WeakReference<View> view;
        IDisposable subscription;

        public static BindableProperty ObservableCollectionProperty =
            BindableProperty.Create(nameof(ObservableCollection), typeof(IList<T>), typeof(CollectionColoringBehavior<T>), propertyChanged: OnCollectionChanged);
        public IList<T> ObservableCollection
        {
            get => (IList<T>)GetValue(ObservableCollectionProperty);
            set
            {
                SetValue(ObservableCollectionProperty, value);
                CalculateColor();
            }
        }

        public static readonly BindableProperty ColorsProperty = BindableProperty.CreateAttached(nameof(Colors), typeof(List<Color>), typeof(CollectionColoringBehavior<T>), new List<Color>());
        public List<Color> Colors
        {
            get => (List<Color>)GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }

        public static BindableProperty MeetingRoomIndexProperty = BindableProperty.Create(nameof(MeetingRoomIndex), typeof(int), typeof(CollectionColoringBehavior<T>), default(int));
        public int MeetingRoomIndex
        {
            get => (int)GetValue(MeetingRoomIndexProperty);
            set => SetValue(MeetingRoomIndexProperty, value);
        }

        public static BindableProperty MeetingRoomCountProperty = BindableProperty.Create(nameof(MeetingRoomCount), typeof(int), typeof(CollectionColoringBehavior<T>), default(int));
        public int MeetingRoomCount
        {
            get => (int)GetValue(MeetingRoomCountProperty);
            set => SetValue(MeetingRoomCountProperty, value);
        }

        static void OnCollectionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (CollectionColoringBehavior<T>)bindable;
            self.subscription?.Dispose();

            var newCollection = newValue as ObservableCollection<T>;
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
            T item;
            if (ObservableCollection == null || view?.TryGetTarget(out sameView) != true || (item = sameView.BindingContext as T) == null)
                return;
            try
            {
                var index = ObservableCollection.IndexOf(item);
                var count = ObservableCollection.Count;
                //var colors = Constants.Colors.MeetingRoomColors;

                var backgroundColor = Colors[(count - index - 1) % Colors.Count];

                sameView.BackgroundColor = backgroundColor;
            }
            catch
            {
                // Let's not crash because of a coloring fail :)
            }
        }
    }
}
