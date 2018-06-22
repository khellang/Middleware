using System;
using System.Net;
using System.Threading.Tasks;
using Hellang.Middleware.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Xunit;

namespace RateLimiting.Tests
{
    public class RateLimitingMiddlewareTests
    {
        [Fact]
        public async Task Test()
        {
            RateLimitingOptions Configure(RateLimitingOptions options)
            {
                return options.Limit("requests/ip", limit: 100, period: TimeSpan.FromSeconds(60.0), ctx => ctx.Connection.RemoteIpAddress.ToString());
            }

            var clock = new TestSystemClock
            {
                UtcNow = DateTimeOffset.UtcNow.Date
            };

            using (var server = CreateServer(Configure, clock))
            using (var client = server.CreateClient())
            {
                for (var i = 0; i < 100; i++)
                {
                    using (var response = await client.GetAsync("/"))
                    {
                        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                    }

                    clock.Advance(TimeSpan.FromSeconds(59.0 / 100));
                }

                using (var response = await client.GetAsync("/"))
                {
                    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                }
            }
        }

        private static TestServer CreateServer(Func<RateLimitingOptions, RateLimitingOptions> configure, ISystemClock clock)
        {
            var builder = new WebHostBuilder()
                .UseEnvironment(EnvironmentName.Development)
                .ConfigureServices(x => x
                    .AddDistributedMemoryCache(y => y.Clock = clock)
                    .AddRateLimiting(y => configure(y).Clock = clock))
                .Configure(x => x
                    .UseRemoteAddress(IPAddress.Loopback)
                    .UseRateLimiting());

            return new TestServer(builder);
        }
    }
}
