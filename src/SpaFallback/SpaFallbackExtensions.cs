using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hellang.Middleware.SpaFallback
{
    public static class SpaFallbackExtensions
    {
        private const string MarkerKey = "middleware.SpaFallback";

        public static IServiceCollection AddSpaFallback(this IServiceCollection services)
        {
            return services.AddSpaFallback(configure: null);
        }

        public static IServiceCollection AddSpaFallback(this IServiceCollection services, PathString fallbackPath)
        {
            if (!fallbackPath.HasValue)
            {
                throw new ArgumentException("Fallback path must have a value.", nameof(fallbackPath));
            }

            PathString Factory(HttpContext context)
            {
                return fallbackPath;
            }

            return services.AddSpaFallback(options => options.FallbackPathFactory = Factory);
        }

        public static IServiceCollection AddSpaFallback(this IServiceCollection services, Action<SpaFallbackOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, StartupFilter>());

            return services;
        }

        public static IApplicationBuilder UseSpaFallback(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.Properties[MarkerKey] = true;

            return app.UseMiddleware<SpaFallbackMiddleware>();
        }

        private class StartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    next(app);

                    if (app.Properties.ContainsKey(MarkerKey))
                    {
                        app.UseMiddleware<SpaFallbackMiddleware.Marker>();
                    }
                };
            }
        }
    }
}
