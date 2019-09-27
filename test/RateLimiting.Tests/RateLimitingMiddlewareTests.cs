using System;
using System.Net;
using System.Threading.Tasks;
using Hellang.Middleware.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Xunit;

#if NETCOREAPP2_2
using Environments = Microsoft.Extensions.Hosting.EnvironmentName;
#else
using Environments = Microsoft.Extensions.Hosting.Environments;
#endif

namespace RateLimiting.Tests
{
    public class RateLimitingMiddlewareTests
    {
        [Fact]
        public async Task Test()
        {
            void Configure(RateLimitingOptions options)
            {
                options.Allow("allow administrators", ctx => ctx.User.IsInRole("Admin"));
                options.Allow("allow loopback", ctx => IPAddress.IsLoopback(ctx.Connection.RemoteIpAddress));

                options.Block("block access to admin endpoint", ctx => ctx.Request.Path.StartsWithSegments("/admin"));

                options.Limit("requests by ip", ctx => ctx.User.Identity.IsAuthenticated ? 5000 : 60, TimeSpan.FromHours(1), ctx => ctx.Connection.RemoteIpAddress.ToString());
            }

            var clock = new TestSystemClock
            {
                UtcNow = DateTimeOffset.UtcNow.Date
            };

            using (var server = CreateServer(Configure, clock))
            using (var client = server.CreateClient())
            {
                for (var i = 0; i < 60; i++)
                {
                    using (var response = await client.GetAsync("/"))
                    {
                        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                    }

                    clock.Advance(TimeSpan.FromSeconds(1));
                }

                using (var response = await client.GetAsync("/"))
                {
                    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                }
            }
        }

        private static TestServer CreateServer(Action<RateLimitingOptions> configure, ISystemClock clock)
        {
            var builder = new WebHostBuilder()
                .UseEnvironment(Environments.Development)
                .ConfigureServices(x => x
                    .AddDistributedMemoryCache(y => y.Clock = clock)
                    .AddRateLimiting(y =>
                    {
                        y.Clock = clock;
                        configure(y);
                    }))
                .Configure(x => x
                    .UseRemoteAddress(IPAddress.Parse("2.2.2.2"))
                    .UseRateLimiting());

            return new TestServer(builder);
        }
    }
}
