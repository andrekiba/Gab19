using System;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage.Table;

namespace Gab.Shared.Models
{
    public class Sub
    {
        public string Id { get; set; }
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string NotificationUrl { get; set; }
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }

    public class CreateSub
    {
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string NotificationUrl { get; set; }
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }

    public class SubEntity : TableEntity
    {
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string NotificationUrl { get; set; }
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }

    public static class Mappings
    {
        public static SubEntity ToSubEntity(this Subscription sub)
        {
            return new SubEntity
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

        public static SubEntity ToSubEntity(this Sub sub)
        {
            return new SubEntity
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

        public static Sub ToSub(this SubEntity sub)
        {
            return new Sub
            {
                Id = sub.RowKey,
                Resource = sub.Resource,
                ChangeType = sub.ChangeType,
                ClientState = sub.ClientState,
                NotificationUrl = sub.NotificationUrl,
                ExpirationDateTime = sub.ExpirationDateTime
            };
        }

    }
}
