using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gab.Shared.Base;
using Gab.Shared.Models;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace Gab.Services
{
    public interface IMeetingRoomsService
    {
        Task<Result<List<MeetingRoom>>> GetMeetingRooms();
        Task<Result<List<Event>>> GetCalendarView(string user, DateTime start, DateTime end, string timeZone);
        Task<Result> CreateEvent(CreateEvent createEvent);
        Task<Result<SignalRConnectionInfo>> GetHubInfo();
    }
}
