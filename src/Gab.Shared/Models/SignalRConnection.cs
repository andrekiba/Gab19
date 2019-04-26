using Newtonsoft.Json;

namespace Gab.Shared.Models
{
    public class SignalRConnection
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }
    }
}
