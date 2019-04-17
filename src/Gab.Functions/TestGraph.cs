using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Gab.Functions
{
    public static class TestGraph
    {
        [FunctionName("TestGraph")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Token(Identity = TokenIdentityMode.UserFromRequest, Resource = "https://graph.microsoft.com")]string graphToken,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphToken);
            return await client.GetAsync("https://graph.microsoft.com/beta/me/");
        }
    }
}
