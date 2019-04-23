using System;
using FormsControls.Base;
using Gab.Base.Grouping;
using Gab.Resources;
using Gab.Shared.Models;
using Gab.ViewModels;
using Syncfusion.DataSource;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Gab.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MeetingRoomPage : ContentPage, IAnimationPage
    {
        public MeetingRoomPage()
        {
            InitializeComponent();

            EventList.ItemTapped += (sender, e) =>
            {
                if (Device.RuntimePlatform == Device.iOS || Device.RuntimePlatform == Device.Android)
                    EventList.SelectedItem = null;
            };

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
                        key.Name = $"{AppResources.TomorrowLabel}  {ev.Start:D}";
                        key.Value = 1;
                    }
                    else
                    {
                        key.Name = ev.Start.ToString("D");
                        key.Value = 2;
                    }
                    key.DateTime = date;                   
                    return key;
                },
                Comparer = new GroupComparer()
            });
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