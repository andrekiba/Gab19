using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gab.Base;
using Gab.Shared.Base;
using Gab.Shared.Messages;
using Gab.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Refit;

namespace Gab.Services
{
    public class MeetingRoomsService : IMeetingRoomsService
    {
        #region Fields

        const string DateFormat = "yyyy-MM-ddTHH:mm:ss";
        HubConnection hubConnection;
        Random random;
        readonly Subject<Event> eventChanged = new Subject<Event>();

        readonly IMeetingRoomsApi api;
        // Riprova 2 volte
        // 2 ^ 1 = 2 => la prima volta dopo 2 secondi
        // 2 ^ 2 = 4 => la seconda volta dopo 4 secondi
        readonly AsyncRetryPolicy retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    //var exType = exception.GetType();
                    //Debug.WriteLine(string.Concat("Policy Error: ", exType.ToString(), " ", exception.Message));
                    // loggare su Insights e HockeyApp
                });
        //se va in timeout oppure è un errore dell'api non tenterà di fare il retry
        //circuit break per 20 secondi
        readonly AsyncCircuitBreakerPolicy breakPolicy = Policy.Handle<OperationCanceledException>()
            .Or<ApiException>()
            .CircuitBreakerAsync(1, TimeSpan.FromSeconds(20),
                (exception, timeSpan, context) =>
                {

                },
                context => { });

        #endregion

        #region Properties

        public bool IsHubConnected { get; private set; }
        public IObservable<Event> WhenEventChanged => eventChanged.AsObservable();

        #endregion

        #region Constructor

        public MeetingRoomsService()
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(Constants.MeetingRoomsApi)
                };

                var serializeSettings = new JsonSerializerSettings
                {
                    //DateFormatString = DateFormat,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    DateParseHandling = DateParseHandling.DateTimeOffset
                };

                api = RestService.For<IMeetingRoomsApi>(client, new RefitSettings
                {
                    ContentSerializer = new JsonContentSerializer(serializeSettings),                   
                    UrlParameterFormatter = new DateUrlParameterFormatter()
                });
            }
            catch (Exception)
            {
                //
            }
        }

        #endregion 

        #region Methods

        public async Task<Result<List<MeetingRoom>>> GetMeetingRooms()
        {
            //var meetingRooms = new List<MeetingRoom>
            //{
            //    new MeetingRoom
            //    {
            //        Id = "0e17c9c5-9a12-47fd-b7dc-44f53a986dd6",
            //        Mail = "daitarn@hyrule.onmicrosoft.com",
            //        Name = "Daitarn"
            //    },
            //    new MeetingRoom
            //    {
            //        Id = "9bc83951-f968-4ab7-b07d-effb484205ad",
            //        Mail = "voltron@hyrule.onmicrosoft.com",
            //        Name = "Voltron"
            //    },
            //    new MeetingRoom
            //    {
            //        Id = "b8817ade-6fae-410f-8dec-7932abbb8029",
            //        Mail = "mazinga@hyrule.onmicrosoft.com",
            //        Name = "Mazinga"
            //    }
            //};

            //return await Task.FromResult(Result.Ok(meetingRooms));

            return await BreakOrRetry(async () => await api.GetMeetingRooms(Constants.MeetingRoomsFuncKey, GetCancellationToken()));
        }

        public async Task<Result<List<Event>>> GetCalendarView(string user, DateTime start, DateTime end, string timeZone)
        {
            //var events = new List<Event>
            //{
            //    new Event
            //    {
            //        Id = Guid.Empty.ToString(),
            //        Subject = "Test Meeting 1",
            //        BodyPreview = "Ciao ciao!",
            //        Start = DateTime.Parse("2019-04-23T13:00:00"),
            //        End = DateTime.Parse("2019-04-23T14:30:00"),
            //        Organizer = "Andrea Ceroni",
            //        TimeZone = "W. Europe Standard Time"
            //    },
            //    new Event
            //    {
            //        Id = Guid.Empty.ToString(),
            //        Subject = "Test Meeting 2 con un titolo davvero molto lungo che non ci sta in una riga sola e nemmeno in due, ma neanche in tre righe penso",
            //        BodyPreview = "No preview",
            //        Start = DateTime.Parse("2019-04-23T18:00:00"),
            //        End = DateTime.Parse("2019-04-23T22:43:00"),
            //        Organizer = "Andrea Ceroni",
            //        TimeZone = "W. Europe Standard Time"
            //    },
            //    new Event
            //    {
            //        Id = Guid.Empty.ToString(),
            //        Subject = "Test Meeting 3",
            //        BodyPreview = "No preview",
            //        Start = DateTime.Parse("2019-04-23T23:00:00"),
            //        End = DateTime.Parse("2019-04-23T23:30:00"),
            //        Organizer = "Andrea Ceroni",
            //        TimeZone = "W. Europe Standard Time"
            //    },
            //    new Event
            //    {
            //        Id = Guid.Empty.ToString(),
            //        Subject = "Test Meeting 4",
            //        BodyPreview = "Ciao ciao 2!",
            //        Start = DateTime.Parse("2019-04-24T15:00:00"),
            //        End = DateTime.Parse("2019-04-24T16:00:00"),
            //        Organizer = "Andrea Ceroni",
            //        TimeZone = "W. Europe Standard Time"
            //    },
            //};

            //return await Task.FromResult(Result.Ok(events));

            return await BreakOrRetry(async () => await api.GetCalendarView(Constants.MeetingRoomsFuncKey, user, start.ToString("s"), end.ToString("s"), timeZone, GetCancellationToken()));
        }

        public async Task<Result<Event>> CreateEvent(CreateEvent createEvent)
        {
            //return await Task.FromResult(Result.Ok());

            return await BreakOrRetry(async () => await api.CreateEvent(Constants.MeetingRoomsFuncKey, createEvent, GetCancellationToken()));
        }

        public async Task<Result> EndsEvent(EndsEvent endsEvent)
        {
            //return await Task.FromResult(Result.Ok());

            return await BreakOrRetry(async () => await api.EndsEvent(Constants.MeetingRoomsFuncKey, endsEvent, GetCancellationToken()));
        }

        public async Task<Result> Subscribe(CreateSubscription createSubscription)
        {
            //return await Task.FromResult(Result.Ok());

            return await BreakOrRetry(async () => await api.Subscribe(Constants.MeetingRoomsFuncKey, createSubscription, GetCancellationToken()));
        }

        public async Task<Result<SignalRConnection>> GetHubInfo()
        {
            //var connectionInfo = new SignalRConnectionInfo
            //{
            //    Url = "",
            //    AccessToken = ""
            //};

            //return await Task.FromResult(Result.Ok(connectionInfo));

            return await BreakOrRetry(async () => await api.GetHubInfo(Constants.MeetingRoomsFuncKey, GetCancellationToken()));
        }

        public async Task<Result> AddToHubGroup(string userId)
        {
            //return await Task.FromResult(Result.Ok());
            return await BreakOrRetry(async () => await api.AddToHubGroup(Constants.MeetingRoomsFuncKey, userId, GetCancellationToken()));
        }

        public Task<Result> ConfigureHub()
        {
            try
            {
                random = new Random();
                //var connectionInfo = await GetHubInfo();
                //if (connectionInfo.IsFailure)
                //    return connectionInfo;

                hubConnection = new HubConnectionBuilder()
                    .WithUrl(Constants.MeetingRoomsApi)
                    .Build();

                hubConnection.Closed += async (error) =>
                {
                    IsHubConnected = false;
                    await Task.Delay(random.Next(0, 5) * 1000);
                    try
                    {
                        await ConnectHub();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                };

                hubConnection.On<Event>(HubMessages.EventChanged,  e =>
                {
                    eventChanged.OnNext(e);
                });

                return Task.FromResult(Result.Ok());
            }
            catch (Exception e)
            {
                return Task.FromResult(Result.Fail(e.Message));
            }
        }

        public async Task<Result> ConnectHub()
        {
            try
            {
                if (IsHubConnected)
                    return Result.Ok();

                await hubConnection.StartAsync();

                IsHubConnected = true;

                return Result.Ok();
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }
        }

        public async Task<Result> DisconnectHub()
        {
            try
            {
                if (!IsHubConnected)
                    return Result.Ok();
                await hubConnection.StopAsync();
                IsHubConnected = false;
                return Result.Ok();
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }
        }

        Task<T> BreakOrRetry<T>(Func<Task<T>> func)
        {
            //return retryPolicy.ExecuteAsync(() => breakPolicy.ExecuteAsync(func));
            return Policy.WrapAsync(retryPolicy, breakPolicy).ExecuteAsync(func);
        }

        static CancellationToken GetCancellationToken()
        {
            //token di defualt con timeout di 1 minuto
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromMinutes(1));
            return tokenSource.Token;
        }

        #endregion

        #region Utility

        public class DateUrlParameterFormatter : DefaultUrlParameterFormatter
        {
            public override string Format(object parameterValue, ParameterInfo parameterInfo)
            {
                if (parameterValue == null)
                    return null;

                var date = parameterValue as DateTimeOffset?;
                if (date.HasValue)
                    return date.Value.ToString(DateFormat, CultureInfo.InvariantCulture);

                date = parameterValue as DateTime?;

                return date?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? base.Format(parameterValue, parameterInfo);
            }
        }

        #endregion 
    }
}
