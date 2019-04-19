using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Gab.Functions.Configuration;
using Gab.Shared.Base;
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
using ILogger = Serilog.ILogger;
using Subscription = Microsoft.Graph.Subscription;

namespace Gab.Functions
{
    public class GraphSubscription
    {
        #region Field

        readonly AppSettings configuration;
        readonly ILogger log;

        #endregion 

        public GraphSubscription(AppSettings configuration)
        {
            this.configuration = configuration;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(GraphSubscription)}Log")
                .CreateLogger();
        }

        #region Functions

        [FunctionName("Subscribe")]
        public async Task<IActionResult> Subscribe(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscription")] HttpRequest req,
            [Table("subscriptions", Connection = "AzureWebJobsStorage")] IAsyncCollector<SubscriptionEntity> subTable)        
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var sub = JsonConvert.DeserializeObject<CreateSubscription>(requestBody);

                var subscription = new Subscription
                {
                    ChangeType = sub.ChangeType,
                    ClientState = sub.ClientState,
                    ExpirationDateTime = sub.ExpirationDateTime,
                    NotificationUrl = sub.NotificationUrl,
                    Resource = sub.Resource
                };

                var createdSub = await graphClient.Subscriptions.Request().AddAsync(subscription);

                await subTable.AddAsync(createdSub.ToSubscriptionEntity());

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
                var ev = await graphClient.Users[user].Events[notification.Resource.Id].Request().GetAsync();
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = $"Event{notification.ChangeType.UppercaseFirst()}",
                        Arguments = new object[] { ev.ToEvent() }
                    });
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }

        [FunctionName("UpdateSubscriptions")]
        public async Task UpdateSubscriptions(
            [TimerTrigger("12:00:00", RunOnStartup = true)]TimerInfo timer,
            [Table("subscriptions", Connection = "AzureWebJobsStorage")] CloudTable subTable)
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1);

                var query = new TableQuery<SubscriptionEntity>();
                var segment = await subTable.ExecuteQuerySegmentedAsync(query, null);
                var subs = segment.ToList();

                foreach (var sub in subs)
                {
                    var subscription = await graphClient.Subscriptions[sub.RowKey].Request().GetAsync();
                    if (subscription.ExpirationDateTime.HasValue)
                    {
                        if(subscription.ExpirationDateTime.Value - DateTimeOffset.UtcNow < TimeSpan.FromHours(24))
                            subscription.ExpirationDateTime = subscription.ExpirationDateTime.Value.AddDays(1);
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

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetHubInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "gab19")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

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

        async Task<string> GetGraphToken()
        {
            var authContext = new AuthenticationContext(configuration.OpenIdIssuer);
            var result = await authContext.AcquireTokenAsync(configuration.GraphEndpoint, new ClientCredential(configuration.ClientId, configuration.ClientSecret));
            return result.AccessToken;
        }

        #endregion 
    }
}
 