using System;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage.Table;

namespace Gab.Shared.Models
{
    public class Subscription
    {
        public string Id { get; set; }
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string NotificationUrl { get; set; }
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }

    public class CreateSubscription
    {
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string NotificationUrl { get; set; }
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }

    public class SubscriptionEntity : TableEntity
    {
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string NotificationUrl { get; set; }
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }

    public static class SubscriptionMappings
    {
        public static SubscriptionEntity ToSubscriptionEntity(this Microsoft.Graph.Subscription sub)
        {
            return new SubscriptionEntity
            {
                PartitionKey = "SUBSCRIPTION",
                RowKey = sub.Id,
                Resource = sub.Resource,
                ChangeType = sub.ChangeType,
                ClientState = sub.ClientState,
                NotificationUrl = sub.NotificationUrl,
                ExpirationDateTime = sub.ExpirationDateTime
            };
        }

        public static SubscriptionEntity ToSubscriptionEntity(this Subscription sub)
        {
            return new SubscriptionEntity
            {
                PartitionKey = "SUBSCRIPTION",
                RowKey = sub.Id,
                Resource = sub.Resource,
                ChangeType = sub.ChangeType,
                ClientState = sub.ClientState,
                NotificationUrl = sub.NotificationUrl,
                ExpirationDateTime = sub.ExpirationDateTime
            };
        }

        public static Subscription ToSubscription(this SubscriptionEntity se)
        {
            return new Subscription
            {
                Id = se.RowKey,
                Resource = se.Resource,
                ChangeType = se.ChangeType,
                ClientState = se.ClientState,
                NotificationUrl = se.NotificationUrl,
                ExpirationDateTime = se.ExpirationDateTime
            };
        }

    }
}
