﻿using Microsoft.Extensions.Configuration;

namespace Gab.Functions.Configuration
{
    public class AppSettings
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string TenantId { get; }
        public string OpenIdIssuer { get; }
        public string GraphBaseUrl { get; }
        public string GraphV1 { get; }
        public string GraphBeta { get; }
        public string MeetingRoomsApi { get; }
        public string MeetingRoomsUserId { get; }


		readonly IConfigurationRoot config;

        public AppSettings()
        {
            config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            ClientId = config.GetValue<string>("ClientId");
            ClientSecret = config.GetValue<string>("ClientSecret");
            TenantId = config.GetValue<string>("TenantId");
            OpenIdIssuer = config.GetValue<string>("OpenIdIssuer");
            GraphBaseUrl = config.GetValue<string>("GraphBaseUrl");
            GraphV1 = $"{GraphBaseUrl}v1.0";
            GraphBeta = $"{GraphBaseUrl}beta";
            MeetingRoomsApi = config.GetValue<string>("MeetingRoomsApi");
            MeetingRoomsUserId = config.GetValue<string>("MeetingRoomsUserId");
		}

        public string GetValue(string key) => config.GetValue<string>(key);
    }
}
