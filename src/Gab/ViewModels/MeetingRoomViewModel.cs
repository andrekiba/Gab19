using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
        const string CurrentXamarinTimeZone = "Europe/Rome";

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
        public int CurrentEventDuraion => CurrentEvent != null ? (int)(CurrentEvent.End - CurrentEvent.Start).TotalSeconds : 0;

        [DependsOn(nameof(CurrentEvent))]
        public double CurrentEventProgress => CurrentEvent != null ? (DateTime.Now - CurrentEvent.Start).TotalSeconds * 100 / CurrentEventDuraion : 0;

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

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            Setup().ToObservable().Subscribe(
            async setupResult =>
            {
                if (setupResult.IsFailure)
                    await UserDialogs.Instance.AlertAsync(setupResult.Error, AppResources.Error, AppResources.Ok);
            },
            async ex =>
            {
                await UserDialogs.Instance.AlertAsync(ex.Message, AppResources.Error, AppResources.Ok);
            });

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

        async Task<Result> Setup()
        {
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

            var subscription = await mrService.Subscribe(new CreateSubscription
            {
                Resource = $"users/{MeetingRoom.Id}/events",
                ChangeType = "created,updated,deleted",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(2),
                NotificationUrl = Constants.NotificationUrl,
                ClientState = "superSegreto"
            });

            if (subscription.IsFailure)
                return subscription;

            var startHub = await mrService.ConfigureHub()
                .OnSuccess(() => mrService.ConnectHub())
                .OnSuccess(() => SubscribeToHub());

            if (startHub.IsFailure)
                return startHub;

            var addToGroup = await mrService.AddToHubGroup(MeetingRoom.Id);

            return addToGroup.IsFailure ? addToGroup : Result.Ok();
        }

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
                    return await mrService.GetCalendarView(MeetingRoom.Id, DateTime.Today, DateTime.Today.AddDays(2).AddSeconds(-1))
                        .OnFailure(error => Events.Clear())
                        .OnSuccess(events =>
                        {
                            var convEvents = events.Select(e => e.ConvertTimeToTimeZone(CurrentXamarinTimeZone));
                            Events.ReplaceRange(convEvents);
                        });

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
                var createEvent = new CreateEvent
                {
                    MeetingRoom = MeetingRoom,
                    TimeZone = CurrentTimeZone
                };

                var result = await Do(async () => await mrService.CreateEvent(createEvent),
                    AppResources.LoadingMessage, $"{GetType().Name} {nameof(ExecuteCreateEventCommand)}");

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);
                else
                    Events.Add(result.Value.ConvertTimeToTimeZone(CurrentXamarinTimeZone));
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
                Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (e.MeetingRoom != MeetingRoom.Id)
                            return;
                        switch (e.ChangeType)
                        {
                            case ChangeType.Created:
                            case ChangeType.Updated:
                                e.ConvertTimeToTimeZone(CurrentXamarinTimeZone);
                                var ev = Events.SingleOrDefault(x => x.Id == e.Id);
                                if (ev != null)
                                {
                                    ev.Update(e);
                                    SetCurrentEvent();
                                }
                                else
                                    Events.Add(e);
                                break;
                            case ChangeType.Deleted:
                                Events.Remove(e);
                                break;
                            case ChangeType.None:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                });                     
            });
        }

        #endregion 
    }
}
