using Gab.Functions;
using Gab.Functions.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]
namespace Gab.Functions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            //builder.Services.AddSingleton<IConfiguration, AppSettings>();
            builder.Services.AddSingleton<AppSettings>();
        }
    }
}
