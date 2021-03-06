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
using Gab.Shared.Messages;
using Gab.Shared.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Serilog;
using Event = Gab.Shared.Models.Event;
using ILogger = Serilog.ILogger;
using Subscription = Microsoft.Graph.Subscription;

namespace Gab.Functions
{
    public class MeetingRoomsApi
    {
        #region Fields

        readonly AppSettings configuration;
        readonly ILogger log;

        #endregion 

        public MeetingRoomsApi(AppSettings configuration)
        {
            this.configuration = configuration;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(MeetingRoomsApi)}Log")
                .CreateLogger();
        }

        #region Events

        [FunctionName("MeetingRooms")]
        public async Task<IActionResult> MeetingRooms(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetingRooms")] HttpRequest req
            //[Token(Identity = TokenIdentityMode.ClientCredentials, IdentityProvider = "AAD", Resource = "https://graph.microsoft.com")]string token
            )
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);

                //use default user
                var result = await graphClient.Users[configuration.GetValue("MeetingRoomsUserId")].People.Request()
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
				//await client.GetAsync($"https://graph.microsoft.com/beta/users/{configuration.GetValue("MeetingRoomsUserId")}/findRooms");
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

                var events = result.Select(e => e.ToEvent(ChangeType.None, user)).ToList();

                return new OkObjectResult(Result.Ok(events));
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail<List<Event>>(error));
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
                    return new OkObjectResult(Result.Fail<Event>(ErrorMessages.RoomItWillBeBusySoonMessage));

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
                    Subject = $"Prenotazione Manuale {input.MeetingRoom.Name}",
                    BodyPreview =$"Prenotazione Manuale {input.MeetingRoom.Name}",
                    Start = startTime,
                    End = endTime
                });

                return new OkObjectResult(Result.Ok(createdEvent.ToEvent(ChangeType.Created, input.MeetingRoom.Id)));
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail<Event>(error));
            }
        }

        [FunctionName("EndsEvent")]
        public async Task<IActionResult> EndsEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "event/ends")] HttpRequest req)
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<EndsEvent>(requestBody);

                var timeZone = input.TimeZone;

                var endTime = new DateTimeTimeZone
                {
                    DateTime = input.Ended,
                    TimeZone = timeZone
                };

                //var ev = await graphClient.Users[input.MeetingRoom.Mail].Events[input.Id].Request().GetAsync();
                //ev.End = endTime;

                var ev = new Microsoft.Graph.Event
                {
                    End = endTime
                };

                await graphClient.Users[input.MeetingRoom.Mail].Events[input.Id].Request().UpdateAsync(ev);

                return new OkObjectResult(Result.Ok());
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail(error));
            }
        }

        #endregion

        #region Subscriptions

        [FunctionName("Subscribe")]
        public async Task<IActionResult> Subscribe(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe")] HttpRequest req,
            [Table("subscriptions", Connection = "AzureWebJobsStorage")] CloudTable subTable)
        {
            try
            {
                await subTable.CreateIfNotExistsAsync();

                var graphClient = GetGraphClient(configuration.GraphV1);
                
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var createSub = JsonConvert.DeserializeObject<CreateSubscription>(requestBody);

                var subscription = new Subscription
                {
                    ChangeType = createSub.ChangeType,
                    ClientState = createSub.ClientState,
                    ExpirationDateTime = createSub.ExpirationDateTime,
                    NotificationUrl = createSub.NotificationUrl.IsNullOrWhiteSpace() ? 
                        $"{configuration.MeetingRoomsApi}/notification" : 
                        createSub.NotificationUrl,
                    Resource = createSub.Resource
                };

                var existingSubs = (await graphClient.Subscriptions.Request()
                    //.Filter($"resource eq '{subscription.Resource}'")
                    .GetAsync()).ToList();
                var existingSub = existingSubs.SingleOrDefault(s => s.Resource.Equals(subscription.Resource, StringComparison.InvariantCultureIgnoreCase));
                if (existingSub != null)
                {
                    await graphClient.Subscriptions[existingSub.Id].Request().DeleteAsync();

                    var retrieve = TableOperation.Retrieve<SubscriptionEntity>("SUBSCRIPTION", existingSub.Id);
                    var sub = (await subTable.ExecuteAsync(retrieve)).Result;
                    if (sub != null)
                    {
                        var deleteOperation = TableOperation.Delete((SubscriptionEntity)sub);
                        await subTable.ExecuteAsync(deleteOperation);
                    }
                }

                var createdSub = await graphClient.Subscriptions.Request().AddAsync(subscription);
                var insertOperation = TableOperation.Insert(createdSub.ToSubscriptionEntity());
                await subTable.ExecuteAsync(insertOperation);

                log.Information($"Created new subscription with id:{createdSub.Id}");

                return new OkObjectResult(Result.Ok());
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail(error));
            }
        }

        [FunctionName("UpdateSubscriptions")]
        public async Task UpdateSubscriptions(
            [TimerTrigger("12:00:00", RunOnStartup = true)]TimerInfo timer,
            [Table("subscriptions", Connection = "AzureWebJobsStorage")] CloudTable subTable)
        {
            try
            {
                await subTable.CreateIfNotExistsAsync();

                var graphClient = GetGraphClient(configuration.GraphV1);

                var query = new TableQuery<SubscriptionEntity>();
                var segment = await subTable.ExecuteQuerySegmentedAsync(query, null);
                var subs = segment.ToList();

                foreach (var sub in subs)
                {
                    var subscription = await graphClient.Subscriptions[sub.RowKey].Request().GetAsync();
                    if (subscription.ExpirationDateTime.HasValue)
                    {
                        if (subscription.ExpirationDateTime.Value - DateTimeOffset.UtcNow < TimeSpan.FromHours(24))
                            subscription.ExpirationDateTime = subscription.ExpirationDateTime.Value.AddDays(1);
                        else
                            continue;
                    }
                    else
                        subscription.ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(1);

                    var updatedSub = await graphClient.Subscriptions[sub.RowKey].Request().UpdateAsync(subscription);
                    sub.ExpirationDateTime = updatedSub.ExpirationDateTime;
                    var replaceOperation = TableOperation.Replace(sub);
                    await subTable.ExecuteAsync(replaceOperation);

                    log.Information($"Updated existing subscription with id:{sub.RowKey}");
                }
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }

        #endregion

        #region Notifications

        [FunctionName("NotificationHandler")]
        public async Task<IActionResult> NotificationHandler(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notification")] HttpRequest req,
            [Queue("notifications", Connection = "AzureWebJobsStorage")] IAsyncCollector<Notification> notifcations)
        {
            try
            {
                string validationToken = req.Query["validationToken"];

                //one time test subsciption
                if (!validationToken.IsNullOrWhiteSpace())
                    return new OkObjectResult(validationToken);

                var data = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic result = JsonConvert.DeserializeObject(data);
                var not = result.value.First;

                var notification = new Notification
                {
                    Resource = new Resource
                    {
                        Id = not.resourceData.id,
                        Path = not.resource
                    },
                    ChangeType = not.changeType
                };
                await notifcations.AddAsync(notification);

                return new AcceptedResult();
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new ObjectResult(StatusCodes.Status500InternalServerError);
            }
        }     

        [FunctionName("Notifier")]
        public async Task Notifier(
            [QueueTrigger("notifications", Connection = "AzureWebJobsStorage")]Notification notification,
            [SignalR(HubName = "gab19")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);
                var user = notification.Resource.Path.Split('/')[1];
                var changeType = (ChangeType)Enum.Parse(typeof(ChangeType), notification.ChangeType.UppercaseFirst());
                Event ev;
                if (changeType != ChangeType.Deleted)
                {
                    var graphEvent = await graphClient.Users[user].Events[notification.Resource.Id].Request().GetAsync();
                    ev = graphEvent.ToEvent(changeType, user);
                }
                else
                {
                    ev = new Event
                    {
                        Id = notification.Resource.Id,
                        ChangeType = ChangeType.Deleted
                    };
                }

                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        //GroupName = user,
                        Target = HubMessages.EventChanged,
                        Arguments = new object[] { ev }
                    });
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }        

        [FunctionName("Negotiate")]
        public SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "gab19")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("AddToHubGroup")]
        public async Task<IActionResult> AddToHubGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addToHubGroup/{userId}")]HttpRequest req,
            string userId,
            [SignalR(HubName = "gab19")] IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {
            try
            {
                await signalRGroupActions.AddAsync(new SignalRGroupAction
                {
                    UserId = userId,
                    GroupName = userId,
                    Action = GroupAction.Add
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

        #region Test

        [FunctionName("NotifierTest")]
        public async Task NotifierTest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notifierTest")] Notification notification,
            [SignalR(HubName = "gab19")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);
                var user = notification.Resource.Path.Split('/')[1];
                var changeType = (ChangeType)Enum.Parse(typeof(ChangeType), notification.ChangeType.UppercaseFirst());
                Event ev;
                if (changeType != ChangeType.Deleted)
                {
                    var graphEvent = await graphClient.Users[user].Events[notification.Resource.Id].Request().GetAsync();
                    ev = graphEvent.ToEvent(changeType, user);
                }
                else
                {
                    ev = new Event
                    {
                        Id = notification.Resource.Id,
                        ChangeType = ChangeType.Deleted
                    };
                }

                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        //GroupName = user,
                        Target = HubMessages.EventChanged,
                        Arguments = new object[] { ev }
                    });
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }

        [FunctionName("HubInfo")]
        public IActionResult HubInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hubInfo")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "gab19")] SignalRConnectionInfo connectionInfo)
        {
            try
            {
                var connection = new SignalRConnection
                {
                    Url = connectionInfo.Url,
                    AccessToken = connectionInfo.AccessToken
                };
                return new OkObjectResult(Result.Ok(connection));
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                return new OkObjectResult(Result.Fail<List<MeetingRoom>>(error));
            }
        }

        #endregion 

        #endregion

        #region Methods

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
            var result = await authContext.AcquireTokenAsync(configuration.GraphBaseUrl, new ClientCredential(configuration.ClientId, configuration.ClientSecret));
            return result.AccessToken;
        }

        #endregion
    }
}
 