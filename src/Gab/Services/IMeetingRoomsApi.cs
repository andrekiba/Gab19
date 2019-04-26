using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gab.Shared.Base;
using Gab.Shared.Models;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Refit;

namespace Gab.Services
{
    [Headers("Accept: application/json")]
    public interface IMeetingRoomsApi
    {
        [Get("/meetingRooms")]
        Task<Result<List<MeetingRoom>>> GetMeetingRooms([Header("x-functions-key")]string funcKey, CancellationToken cancellationToken);

        [Get("/calendarView?user={user}&start={start}&end={end}&timeZone={timeZone}")]
        Task<Result<List<Event>>> GetCalendarView([Header("x-functions-key")]string funcKey, string user, string start, string end, string timeZone, CancellationToken cancellationToken);

        [Post("/event")]
        Task<Result<Event>> CreateEvent([Header("x-functions-key")]string funcKey, [Body]CreateEvent createEvent, CancellationToken cancellationToken);

        [Post("/event/ends")]
        Task<Result> EndsEvent([Header("x-functions-key")]string funcKey, [Body]EndsEvent endsEvent, CancellationToken cancellationToken);

        [Post("/negotiate")]
        Task<Result<SignalRConnectionInfo>> GetHubInfo([Header("x-functions-key")]string funcKey, CancellationToken cancellationToken);

        [Post("/subscribe")]
        Task<Result> Subscribe([Header("x-functions-key")]string funcKey, [Body]CreateSubscription createSubscription, CancellationToken cancellationToken);

        [Post("/addToHubGroup/{userId}")]
        Task<Result> AddToHubGroup([Header("x-functions-key")]string funcKey, string userId, CancellationToken cancellationToken);
    }
}
