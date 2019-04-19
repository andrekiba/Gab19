using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Gab.Functions.Configuration;
using Gab.Shared.Base;
using Gab.Shared.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Serilog;
using Event = Gab.Shared.Models.Event;
using ILogger = Serilog.ILogger;

namespace Gab.Functions
{
    public class GraphApi
    {
        #region Field

        readonly AppSettings configuration;
        readonly ILogger log;

        #endregion 

        public GraphApi(AppSettings configuration)
        {
            this.configuration = configuration;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(GraphApi)}Log")
                .CreateLogger();
        }

        [FunctionName("MeetingRooms")]
        public async Task<IActionResult> MeetingRooms(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetingRooms")] HttpRequest req
            //[Token(Identity = TokenIdentityMode.ClientCredentials, IdentityProvider = "AAD", Resource = "https://graph.microsoft.com")]string token
            )
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);

                var result = await graphClient.Users["b46397cf-4e6f-4f3d-9134-0d8b70646548"].People.Request()
                    .Filter("personType/subclass eq 'Room'")
                    .GetAsync();

                var meetingRooms = result.Select(p => new MeetingRoom
                {
                    Id = p.Id,
                    Name = p.DisplayName,
                    Mail = p.UserPrincipalName
                }).ToList();

                return new OkObjectResult(Result.Ok(meetingRooms));

                //var client = await GetGraphHttpClient();
                //await client.GetAsync("https://graph.microsoft.com/beta/users/0e17c9c5-9a12-47fd-b7dc-44f53a986dd6/findRooms");
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail<List<MeetingRoom>>(error));
            }
        }

        [FunctionName("CalendarView")]
        public async Task<IActionResult> CalendarView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendarView")] HttpRequest req)
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);

                var user = req.Query["user"];
                var startDateTime = new QueryOption("startDateTime", req.Query["start"]);
                var endDateTime = new QueryOption("endDateTime", req.Query["end"]);             
                var options = new List<Option> { startDateTime, endDateTime };
                if (!string.IsNullOrWhiteSpace(req.Query["timeZone"]))
                {
                    var timeZone = new HeaderOption("Prefer", $"outlook.timezone=\"{req.Query["timeZone"]}\"");
                    options.Add(timeZone);
                }        

                var result = await graphClient.Users[user].CalendarView.Request(options).GetAsync();

                var events = result.Select(e => new Event
                {
                    Id = e.Id,
                    Subject = e.Subject,
                    BodyPreview = e.BodyPreview,
                    Start = e.Start.DateTime,
                    End = e.End.DateTime,
                    Organizer = e.Organizer.EmailAddress.Name,
                    TimeZone = e.OriginalStartTimeZone,
                }).ToList();

                return new OkObjectResult(Result.Ok(events));
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail<List<MeetingRoom>>(error));
            }
        }

        [FunctionName("CreateEvent")]
        public async Task<IActionResult> CreateEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "event")] HttpRequest req)
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<CreateEvent>(requestBody);

                var timeZone = input.TimeZone;

                var client = await GetGraphHttpClient(timeZone);

                var nearestUtc = DateTime.UtcNow.RoundToNearest(TimeSpan.FromMinutes(15));
                var nearest = TimeZoneInfo.ConvertTimeFromUtc(nearestUtc, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                var json = new
                {
                    schedules = new List<string> { input.MeetingRoom.Mail },
                    startTime = new { dateTime = nearest.ToString("s"), timeZone },
                    endTime = new { dateTime = nearest.AddMinutes(30).ToString("s"), timeZone },
                    availabilityViewInterval = "30"
                };
                var getSchedule = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{configuration.GraphBeta}/users/{input.MeetingRoom.Mail}/calendar/getSchedule", getSchedule);
                dynamic result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                if (result.value.First.availabilityView != "0")
                    return new OkObjectResult(Result.Fail("Room is busy!"));

                var startTime = new DateTimeTimeZone
                {
                    DateTime = nearest.ToString("s"),
                    TimeZone = timeZone
                };
                var endTime = new DateTimeTimeZone
                {
                    DateTime = nearest.AddMinutes(30).ToString("s"),
                    TimeZone = timeZone
                };

                var createdEvent = await graphClient.Users[input.MeetingRoom.Mail].Events.Request().AddAsync(new Microsoft.Graph.Event
                {
                    //Subject = $"Event{Guid.NewGuid().ToString().Substring(0, 8)}",
                    Subject = $"Prenotazione Manuale {input.MeetingRoom.Name}",
                    BodyPreview =$"Prenotazione Manuale {input.MeetingRoom.Name}",
                    Start = startTime,
                    End = endTime
                });

                return new OkObjectResult(Result.Ok());
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail(error));
            }
        }

        GraphServiceClient GetGraphClient(string endpoint)
        {
            return new GraphServiceClient(endpoint, new DelegateAuthenticationProvider(
                async rm =>
                {
                    var token = await GetGraphToken();
                    rm.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));
        }
        async Task<HttpClient> GetGraphHttpClient(string timeZone = null)
        {
            var token = await GetGraphToken();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata.metadata=none");
            client.DefaultRequestHeaders.Add("Prefer", timeZone != null ? $"outlook.timezone=\"{timeZone}\"" : "outlook.timezone=\"W. Europe Standard Time\"");

            return client;
        }
        async Task<string> GetGraphToken()
        {
            var authContext = new AuthenticationContext(configuration.OpenIdIssuer);
            var result = await authContext.AcquireTokenAsync(configuration.GraphEndpoint, new ClientCredential(configuration.ClientId, configuration.ClientSecret));
            return result.AccessToken;
        }
    }
}
 