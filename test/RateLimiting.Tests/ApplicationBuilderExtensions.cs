using System.Net;
using Microsoft.AspNetCore.Builder;

namespace RateLimiting.Tests
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRemoteAddress(this IApplicationBuilder app, IPAddress address)
        {
            return app.Use((ctx, next) =>
            {
                ctx.Connection.RemoteIpAddress = address;
                return next();
            });
        }
    }
}
