using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.RateLimiting
{
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, Action<RateLimitingOptions> configure)
        {
            services.Configure(configure);

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RateLimitingOptions>, RateLimitingOptionsSetup>());

            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
