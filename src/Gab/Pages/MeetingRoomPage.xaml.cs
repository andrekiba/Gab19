using System;
using FormsControls.Base;
using Gab.Base.Grouping;
using Gab.Resources;
using Gab.Shared.Models;
using Gab.ViewModels;
using Plugin.Multilingual;
using Syncfusion.DataSource;
using Syncfusion.ListView.XForms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Gab.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MeetingRoomPage : ContentPage, IAnimationPage
    {
        #region Fields

        double currentWidth = 0;
        double currentHeight = 0;

        #endregion

        #region Properties

        public SfListView EventListView;

        #endregion

        public MeetingRoomPage()
        {
            InitializeComponent();

            EventListView = EventList;

            EventList.ItemTapped += (sender, e) =>
            {
                if (Device.RuntimePlatform == Device.iOS || Device.RuntimePlatform == Device.Android)
                    EventList.SelectedItem = null;
            };

            SortAndGroupEventList();
        }

        public void SortAndGroupEventList()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                EventList.DataSource.SortDescriptors.Clear();
                EventList.DataSource.GroupDescriptors.Clear();
                EventList.DataSource.SortDescriptors.Add(new SortDescriptor { PropertyName = "Start", Direction = ListSortDirection.Ascending });
                EventList.DataSource.GroupDescriptors.Add(new GroupDescriptor
                {
                    PropertyName = "Start",
                    KeySelector = obj =>
                    {
                        var ev = (Event)obj;
                        var key = new DateTimeGroupKey();
                        var date = ev.Start.Date;

                        if (date == DateTime.Today)
                        {
                            key.Name = AppResources.TodayLabel;
                            key.Value = 0;
                        }
                        else if (date == DateTime.Today.AddDays(1))
                        {
                            key.Name = $"{AppResources.TomorrowLabel}  {ev.Start.ToString("dddd, dd MMMM", CrossMultilingual.Current.CurrentCultureInfo)}";
                            key.Value = 1;
                        }
                        else
                        {
                            key.Name = ev.Start.ToString("dddd, dd MMMM", CrossMultilingual.Current.CurrentCultureInfo);
                            key.Value = 2;
                        }
                        key.DateTime = date;
                        return key;
                    },
                    Comparer = new GroupComparer()
                });
            });
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height); //must be called

            if (currentWidth != width || currentHeight != height)
            {
                currentWidth = width;
                currentHeight = height;

                //portrait
                if (currentHeight > currentWidth)
                {
                    ContentLayout.Direction = FlexDirection.Column;

                    BookedLayout.Padding = new Thickness(0, 10, 0, 5);
                    FreeLayout.Padding = new Thickness(0, 10, 0, 5);

                    HeaderLayout.HeightRequest = Device.RuntimePlatform == Device.Android ? 40 : 50;

                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        //FlexLayout.SetGrow(BookedLayout, 0.6f);
                        //FlexLayout.SetGrow(FreeLayout, 0.6f);
                        //FlexLayout.SetGrow(PullToRefresh, 1);

                        FlexLayout.SetBasis(BookedLayout, new FlexBasis(0.4f, true));
                        FlexLayout.SetBasis(FreeLayout, new FlexBasis(0.4f, true));
                        FlexLayout.SetBasis(PullToRefresh, new FlexBasis(0.6f, true));
                    }
                    else
                    {
                        //FlexLayout.SetGrow(BookedLayout, 0.8f);
                        //FlexLayout.SetGrow(FreeLayout, 0.8f);
                        //FlexLayout.SetGrow(PullToRefresh, 1);

                        FlexLayout.SetBasis(BookedLayout, new FlexBasis(0.45f, true));
                        FlexLayout.SetBasis(FreeLayout, new FlexBasis(0.45f, true));
                        FlexLayout.SetBasis(PullToRefresh, new FlexBasis(0.55f, true));
                    }
                }
                else
                {
                    ContentLayout.Direction = FlexDirection.Row;

                    HeaderLayout.HeightRequest = 40;

                    BookedLayout.Padding = new Thickness(0, 10, 0, 10);
                    FreeLayout.Padding = new Thickness(0, 10, 0, 10);

                    //FlexLayout.SetGrow(BookedLayout, 1);
                    //FlexLayout.SetGrow(FreeLayout, 1);
                    //FlexLayout.SetGrow(PullToRefresh, 1f);

                    FlexLayout.SetBasis(BookedLayout, new FlexBasis(0.55f, true));
                    FlexLayout.SetBasis(FreeLayout, new FlexBasis(0.55f, true));
                    FlexLayout.SetBasis(PullToRefresh, new FlexBasis(0.45f, true));
                }
            }
        }

        void PullToRefresh_OnRefreshing(object sender, EventArgs e)
        {
            ((MeetingRoomViewModel)BindingContext).RefreshCommand.Execute(null);
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