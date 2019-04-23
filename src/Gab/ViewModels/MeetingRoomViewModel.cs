using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Gab.Base;
using Gab.Pages;
using Gab.Resources;
using Gab.Services;
using Gab.Shared.Base;
using Gab.Shared.Models;
using MvvmHelpers;
using Plugin.Connectivity;
using PropertyChanged;
using Xamarin.Forms;
using ScrollToPosition = Syncfusion.ListView.XForms.ScrollToPosition;

namespace Gab.ViewModels
{
    public class MeetingRoomViewModel : BaseViewModel
    {
        #region Fields

        readonly IMeetingRoomsService mrService;
        //IDisposable timeTimerSub;
        IDisposable eventChangedSub;
        IDisposable eventsChangedSub;
        const string CurrentTimeZone = "W. Europe Standard Time";

        #endregion

        #region Properties

        public ObservableRangeCollection<Event> Events { get; set; } = new ObservableRangeCollection<Event>();     
        public MeetingRoom MeetingRoom { get; private set; }
        public bool IsRefreshing { get; set; }

        public DateTime Now { get; set; } = DateTime.Now;

        public Event CurrentEvent { get; set; }

        [DependsOn(nameof(CurrentEvent))]
        public bool Booked => CurrentEvent != null;

        [DependsOn(nameof(CurrentEvent))]
        //public int CurrentEventDuraion => CurrentEvent != null ? (int)(CurrentEvent.Start - CurrentEvent.End).TotalSeconds : 0;
        public int CurrentEventDuraion
        {
            get
            {
                if (CurrentEvent == null)
                    return 0;

                var duration = (int) (CurrentEvent.End - CurrentEvent.Start).TotalSeconds;
                return duration;
            }   
        }

        [DependsOn(nameof(CurrentEvent))]
        //public double CurrentEventProgress => CurrentEvent != null ? (DateTime.Now - CurrentEvent.Start).TotalSeconds * 100 / CurrentEventDuraion : 0;
        public double CurrentEventProgress
        {
            get
            {
                if (CurrentEvent == null)
                    return 0;

                var progress = (DateTime.Now - CurrentEvent.Start).TotalSeconds * 100 / CurrentEventDuraion;
                return progress;
            }
        }

        public Color MeetingRoomColor => MeetingRoom != null ? Color.FromHex(MeetingRoom.Color) : (Color)Application.Current.Resources["GrayDark"];
        public Color FreeColor => (Color)Application.Current.Resources["LightGreen"];
        public Color FreeDarkColor => FreeColor.Darker();
        public Color BookedColor => (Color)Application.Current.Resources["LightRed"];
        public Color BookedDarkColor => BookedColor.Darker();

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
        }

        protected override async void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            Device.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                Now = DateTime.Now;
                SetCurrentEvent();
                return true;
            });
            //timeTimerSub = Observable.Timer(TimeSpan.FromMinutes(1)).Subscribe(x =>
            //{
            //    Now = DateTime.Now;
            //    SetCurrentEvent();
            //});
            eventsChangedSub = Events.WhenCollectionChanged().Subscribe(x =>
            {
                SetCurrentEvent();
            });

            await mrService.ConfigureHub();
            await mrService.ConnectHub();
            SubscribeToHub();

            RefreshCommand.Execute(null);
        }

        protected override async void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);
            
            //timeTimerSub.Dispose();
            eventsChangedSub.Dispose();

            await mrService.DisconnectHub();
            eventChangedSub.Dispose();
        }

        #endregion

        #region Commands

        ICommand refreshCommand;
        public ICommand RefreshCommand => refreshCommand ?? (refreshCommand = new Command(async () => await ExecuteRefreshCommand(), () => !IsRefreshing));

        ICommand createEventCommand;
        public ICommand CreateEventCommand => createEventCommand ?? (createEventCommand = new DependentCommand(
                                                async () => await ExecuteCreateEventCommand(),
                                                () => CurrentEvent is null,
                                                this,
                                                () => CurrentEvent));

        ICommand endsEventCommand;
        public ICommand EndsEventCommand => endsEventCommand ?? (endsEventCommand = new DependentCommand(
                                                  async () => await ExecuteEndsEventCommand(),
                                                  () => Booked,
                                                  this,
                                                  () => Booked));
        #endregion

        #region Methods

        void SetCurrentEvent()
        {
            var newCurrentEvent = Events.SingleOrDefault(e => e.Start < Now && e.End > Now);

            if(CurrentEvent == newCurrentEvent)
                return;

            if (CurrentEvent != null)
                CurrentEvent.IsCurrent = false;

            CurrentEvent = newCurrentEvent;
            if (CurrentEvent != null)
            {
                CurrentEvent.IsCurrent = true;
                //Events.RemoveRange(Events.Where(e => e.End.AddSeconds(-1) < CurrentEvent.Start));
                Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                {
                    //((MeetingRoomPage)CurrentPage).EventListView.ScrollTo(CurrentEvent, ScrollToPosition.Start, true);
                    ((MeetingRoomPage)CurrentPage).EventListView.LayoutManager.ScrollToRowIndex(Events.IndexOf(CurrentEvent), ScrollToPosition.Start, true);
                });
            }            
        }
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
                    return await mrService.GetCalendarView(MeetingRoom.Id, DateTime.Today, DateTime.Today.AddDays(2).AddSeconds(-1), CurrentTimeZone)
                        .OnFailure(error => Events.Clear())
                        .OnSuccess(events => Events.ReplaceRange(events));
                        //.OnSuccess(events => Events = new ObservableRangeCollection<Event>(events));

                }, AppResources.LoadingMessage, $"{GetType().Name} {nameof(ExecuteRefreshCommand)}");

                IsRefreshing = false;

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);               
            }
        }
        async Task ExecuteCreateEventCommand()
        {
            if (!CrossConnectivity.Current.IsConnected)
            {
                await UserDialogs.Instance.AlertAsync(AppResources.OfflineMessage, AppResources.Warning, AppResources.Ok);
            }
            else
            {
                var result = await Do(async () => await mrService.CreateEvent(new CreateEvent
                {
                    MeetingRoom = MeetingRoom,
                    TimeZone = CurrentTimeZone
                }), AppResources.LoadingMessage, $"{GetType().Name} {nameof(ExecuteCreateEventCommand)}");

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);
            }
        }
        async Task ExecuteEndsEventCommand()
        {
            if (!CrossConnectivity.Current.IsConnected)
            {
                await UserDialogs.Instance.AlertAsync(AppResources.OfflineMessage, AppResources.Warning, AppResources.Ok);
            }
            else
            {
                var result = await Do(async () => await mrService.EndsEvent(new EndsEvent
                {
                    Id = CurrentEvent.Id,
                    MeetingRoom = MeetingRoom,
                    Ended = DateTime.Now,
                    TimeZone = CurrentTimeZone
                }), AppResources.LoadingMessage, $"{GetType().Name} {nameof(ExecuteEndsEventCommand)}");

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);
                else
                    CurrentEvent.IsCurrent = false;
            }
        }
        void SubscribeToHub()
        {
            eventChangedSub = mrService.WhenEventChanged.Subscribe(e =>
            {
                switch (e.ChangeType)
                {
                    case ChangeType.Created:
                        Events.Add(e);
                        break;
                    case ChangeType.Updated:
                        Events.Replace(e);
                        break;
                    case ChangeType.Deleted:
                        Events.Remove(e);
                        break;
                    case ChangeType.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        #endregion 
    }
}
