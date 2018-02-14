using Hellang.Middleware.SpaFallback;
using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(SpaFallbackHostingStartup))]

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => services.AddSpaFallback());
        }
    }
}
