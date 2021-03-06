﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gab.Shared.Base;
using Gab.Shared.Models;

namespace Gab.Services
{
    public interface IMeetingRoomsService
    {
        bool IsHubConnected { get; }
        IObservable<Event> WhenEventChanged { get; }

        Task<Result<List<MeetingRoom>>> GetMeetingRooms();
        Task<Result<List<Event>>> GetCalendarView(string user, DateTime start, DateTime end, string timeZone = null);
        Task<Result<Event>> CreateEvent(CreateEvent createEvent);
        Task<Result> EndsEvent(EndsEvent endsEvent);
        Task<Result> Subscribe(CreateSubscription createSubscription);

        Task<Result<SignalRConnection>> GetHubInfo();
        Task<Result> AddToHubGroup(string userId);
        Task<Result> ConfigureHub();
        Task<Result> ConnectHub();
        Task<Result> DisconnectHub();
    }
}
