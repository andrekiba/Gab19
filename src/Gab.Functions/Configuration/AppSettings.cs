using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Gab.Functions.Configuration
{
    public class AppSettings
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string TenantId { get; }
        public string OpenIdIssuer { get; }
        public string GraphEndpoint { get; }
        public string GraphV1 { get; }
        public string GraphBeta { get; }


        readonly IConfigurationRoot config;

        public AppSettings()
        {
            config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            ClientId = config.GetValue<string>("WEBSITE_AUTH_CLIENT_ID");
            ClientSecret = config.GetValue<string>("WEBSITE_AUTH_CLIENT_SECRET");
            TenantId = config.GetValue<string>("TenantId");
            OpenIdIssuer = config.GetValue<string>("WEBSITE_AUTH_OPENID_ISSUER");
            GraphEndpoint = config.GetValue<string>("GraphEndpoint");
            GraphV1 = $"{GraphEndpoint}v1.0";
            GraphBeta = $"{GraphEndpoint}beta";
        }

        public string GetValue(string key) => config.GetValue<string>(key);
    }
}
