using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Serilog;
using ILogger = Serilog.ILogger;
using Event = Microsoft.Graph.Event;

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

        [FunctionName("Subscribe")]
        public async Task<IActionResult> Subscribe(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscription")] HttpRequest req,
            [Token(Identity = TokenIdentityMode.ClientCredentials, IdentityProvider = "AAD", Resource = "https://graph.microsoft.com")]string token,
            [Table("subscriptions", Connection = "AzureWebJobsStorage")] IAsyncCollector<SubEntity> subTable,
            
            IBinder binder)
            //[GraphWebhookSubscription(
            //    Identity = TokenIdentityMode.ClientCredentials,
            //    IdentityProvider = "AAD",
            //    SubscriptionResource = "users/0e17c9c5-9a12-47fd-b7dc-44f53a986dd6/calendar/events",
            //    ChangeTypes = new[] { GraphWebhookChangeType.Created, GraphWebhookChangeType.Updated, GraphWebhookChangeType.Deleted },
            //    Action = GraphWebhookSubscriptionAction.Create)] out string clientState)        
        {
            try
            {
                var graphClient = GetGraphClient(configuration.GraphV1, token);
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var sub = JsonConvert.DeserializeObject<CreateSub>(requestBody);

                //var subscription = new Microsoft.Graph.Subscription
                //{
                //    ChangeType = "updated,deleted",
                //    ClientState = "mysecret",
                //    ExpirationDateTime = DateTime.UtcNow.AddDays(2),
                //    NotificationUrl = "your notification url",
                //    Resource = "Users"
                //};

                var subscription = new Subscription
                {
                    ChangeType = sub.ChangeType,
                    ClientState = sub.ClientState,
                    ExpirationDateTime = sub.ExpirationDateTime,
                    NotificationUrl = sub.NotificationUrl,
                    Resource = sub.Resource
                };

                var createdSub = await graphClient.Subscriptions.Request().AddAsync(subscription);

                //var dynamicBlobBinding = new BlobAttribute($"subscriptions/{createdSub.Id}");
                //using (var writer = binder.Bind<TextWriter>(dynamicBlobBinding))
                //{
                //    writer.Write(JsonConvert.SerializeObject(createdSub));
                //}

                await subTable.AddAsync(createdSub.ToSubEntity());

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

        [FunctionName("OnEventChange")]
        public void OnEventChange([GraphWebhookTrigger(ResourceType = "#Microsoft.Graph.Event")] Event ev)
        {
            try
            {
                var id = ev.Id;
                var calendar = ev.Calendar.Name;
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
            }
        }

        [FunctionName("UpdateSubscriptions")]
        public async Task UpdateSubscriptions(
            [TimerTrigger("00:01:00", RunOnStartup = true)]TimerInfo timer,
            [Token(Identity = TokenIdentityMode.ClientCredentials, IdentityProvider = "AAD", Resource = "https://graph.microsoft.com")]string token,
            [Table("subscriptions", Connection = "AzureWebJobsStorage")] CloudTable subTable)
        {
            var graphClient = GetGraphClient(configuration.GraphBeta, token);

            var query = new TableQuery<SubEntity>();
            var segment = await subTable.ExecuteQuerySegmentedAsync(query, null);
            var subs = segment.ToList();

            foreach (var sub in subs)
            {
                var subscription = await graphClient.Subscriptions[sub.RowKey].Request().GetAsync();
                subscription.ExpirationDateTime = subscription.ExpirationDateTime?.AddDays(1) ?? DateTimeOffset.UtcNow.AddDays(1);
                var updatedSub = await graphClient.Subscriptions[sub.RowKey].Request().UpdateAsync(subscription);
                sub.ExpirationDateTime = updatedSub.ExpirationDateTime;
                var replaceOperation = TableOperation.Replace(sub);
                await subTable.ExecuteAsync(replaceOperation);

                log.Information($"Updated existing subscription with id:{sub.RowKey}");
            }
        }

        static GraphServiceClient GetGraphClient(string endpoint, string token)
        {
            return new GraphServiceClient(endpoint, new DelegateAuthenticationProvider(
                rm =>
                {
                    rm.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                    return Task.CompletedTask;
                }));
        }
    }
}
 