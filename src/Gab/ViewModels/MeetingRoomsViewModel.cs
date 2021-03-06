﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Gab.Base;
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
        IDisposable coloringSub;

		#endregion

		#region Properties

		public ObservableRangeCollection<MeetingRoom> MeetingRooms { get; set; } = new ObservableRangeCollection<MeetingRoom>();

        public bool IsRefreshing { get; set; }

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

            coloringSub = MeetingRooms.WhenCollectionChanged().Subscribe(x =>
            {
	            SetColors(MeetingRooms);
            });

			if (!refreshNeeded)
                return;

            refreshNeeded = false;
            refreshTimer.Start();
            RefreshCommand.Execute(null);
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
	        base.ViewIsDisappearing(sender, e);

	        coloringSub?.Dispose();
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
                        //.OnSuccess(SetColors)
                        .OnSuccess(mrs => MeetingRooms.ReplaceRange(mrs));

                }, caller: $"{GetType().Name} {nameof(ExecuteRefreshCommand)}");

                IsRefreshing = false;

                if (result.IsFailure)
                    await UserDialogs.Instance.AlertAsync(result.Error, AppResources.Error, AppResources.Ok);
            }
        }

        static void SetColors(ICollection<MeetingRoom> meetingRooms)
        {
	        var colors = Constants.Colors.MeetingRoomColors;

	        foreach (var (mr, index) in meetingRooms.Select((mr, i) => (mr, i)))
	        {
		        var color = colors[(meetingRooms.Count - index - 1) % colors.Count];
		        mr.Color = color.ToHex();
	        }
        }

		#endregion
	}
}
