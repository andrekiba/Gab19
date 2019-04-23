using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Gab.Resources;
using Gab.Services;
using Gab.Shared.Base;
using Gab.Shared.Models;
using MvvmHelpers;
using Plugin.Connectivity;
using Xamarin.Forms;
using Timer = System.Timers.Timer;

namespace Gab.ViewModels
{
    public class MeetingRoomsViewModel : BaseViewModel
    {
        #region Fields

        readonly IMeetingRoomsService mrService;
        Timer refreshTimer;
        bool refreshNeeded = true;

        #endregion

        #region Properties

        public ObservableRangeCollection<MeetingRoom> MeetingRooms { get; } = new ObservableRangeCollection<MeetingRoom>();

        public bool IsRefreshing { get; set; }

        public MeetingRoom SelectedMeetingRoom { get; set; }

        #endregion

        #region Lifecycle

        public MeetingRoomsViewModel(IMeetingRoomsService mrService)
        {
            this.mrService = mrService;
        }

        public override void Init(object initData)
        {
            refreshTimer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds) { Enabled = true };
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
        }

        #endregion

        #region Commands

        ICommand refreshCommand;
        public ICommand RefreshCommand => refreshCommand ?? (refreshCommand = new Command(async () => await ExecuteRefreshCommand(), () => !IsRefreshing));

        ICommand meetingRoomCommand;
        public ICommand MeetingRoomCommand => meetingRoomCommand ?? (meetingRoomCommand = new Command<object>(async obj =>
        {
            var itemData = ((Syncfusion.ListView.XForms.ItemTappedEventArgs)obj).ItemData;

            if (itemData is MeetingRoom meetingRoom)
                await CoreMethods.PushPageModel<MeetingRoomViewModel>(meetingRoom);
        }));

        #endregion

        #region Methods

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
                    return await mrService.GetMeetingRooms()
                        .OnFailure(error => MeetingRooms.Clear())
                        //.OnSuccess(mrs => mrs.ForEach(x => MeetingRooms.Add(x)));
                        .OnSuccess(mrs => MeetingRooms.ReplaceRange(mrs));
                    
                }, AppResources.LoadingMessage, $"{GetType().Name} {nameof(ExecuteRefreshCommand)}");

                IsRefreshing = false;

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);
            }
        }

        #endregion 
    }
}
