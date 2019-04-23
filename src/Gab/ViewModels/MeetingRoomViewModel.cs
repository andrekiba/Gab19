using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Acr.UserDialogs;
using Gab.Resources;
using Gab.Services;
using Gab.Shared.Base;
using Gab.Shared.Models;
using MvvmHelpers;
using Plugin.Connectivity;
using PropertyChanged;
using Xamarin.Forms;

namespace Gab.ViewModels
{
    public class MeetingRoomViewModel : BaseViewModel
    {
        #region Fields

        readonly IMeetingRoomsService mrService;
        Timer refreshTimer;
        bool refreshNeeded = true;
        IDisposable timeTimerSub;

        #endregion

        #region Properties

        public ObservableRangeCollection<Event> Events { get; set; } = new ObservableRangeCollection<Event>();     
        public MeetingRoom MeetingRoom { get; private set; }
        public bool IsRefreshing { get; set; }

        [DependsOn(nameof(Events), nameof(Now))]
        public Event CurrentEvent => Events.SingleOrDefault(e => e.Start <= DateTime.Now && e.End >= DateTime.Now);

        [DependsOn(nameof(CurrentEvent))]
        public bool Booked => CurrentEvent != null;

        public DateTime Now { get; set; } = DateTime.Now;
        
        #endregion

        #region Lifecycle

        public MeetingRoomViewModel(IMeetingRoomsService mrService)
        {
            this.mrService = mrService;            
        }

        public override void Init(object initData)
        {
            if (!(initData is MeetingRoom meetingRoom))
                return;

            MeetingRoom = meetingRoom;

            refreshTimer = new Timer(TimeSpan.FromMinutes(10).TotalMilliseconds) { Enabled = true };
            refreshTimer.Elapsed += (sender, e) =>
            {
                refreshTimer.Stop();
                refreshNeeded = true;
            };
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);
            if (!refreshNeeded)
                return;

            refreshNeeded = false;
            refreshTimer.Start();
            RefreshCommand.Execute(null);

            timeTimerSub = Observable.Timer(TimeSpan.FromMinutes(1)).Subscribe(x => Now = DateTime.Now);
            Events.CollectionChanged += EventsCollectionChanged();
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);
            Events.CollectionChanged -= EventsCollectionChanged();
            timeTimerSub.Dispose();
        }

        #endregion

        #region Commands

        ICommand refreshCommand;
        public ICommand RefreshCommand => refreshCommand ?? (refreshCommand = new Command(async () => await ExecuteRefreshCommand(), () => !IsRefreshing));

        #endregion

        #region Methods

        NotifyCollectionChangedEventHandler EventsCollectionChanged() => (sender, args) => RaisePropertyChanged(nameof(CurrentEvent));

        async Task ExecuteRefreshCommand()
        {
            if (!CrossConnectivity.Current.IsConnected)
            {
                await UserDialogs.Instance.AlertAsync(AppResources.OfflineMessage, AppResources.Warning, AppResources.Ok);
            }
            else
            {
                IsRefreshing = true;

                var result = await Do(async () =>
                {
                    return await mrService.GetCalendarView(MeetingRoom.Id, DateTime.Today, DateTime.Today.AddDays(2).AddSeconds(-1), "W. Europe Standard Time")
                        .OnFailure(error => Events.Clear())
                        //OnSuccess(events => Events.ReplaceRange(events));
                        .OnSuccess(events =>
                        {
                            Events = new ObservableRangeCollection<Event>(events);
                        });

                }, AppResources.LoadingMessage, $"{GetType().Name} {nameof(ExecuteRefreshCommand)}");

                IsRefreshing = false;

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);
            }
        }

        #endregion 
    }
}
