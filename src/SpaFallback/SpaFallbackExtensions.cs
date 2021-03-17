using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Microsoft.AspNetCore.Http.HttpMethods;

namespace Hellang.Middleware.SpaFallback
{
    public static class SpaFallbackExtensions
    {
        private const string UseMiddleware = nameof(IApplicationBuilder) + "." + nameof(UseSpaFallback);

        private const string AddServices = nameof(IServiceCollection) + "." + nameof(AddSpaFallback);

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

            return services.AddSpaFallback(options => options.GetFallbackPath = ctx => fallbackPath);
        }

        public static IServiceCollection AddSpaFallback(this IServiceCollection services, Action<SpaFallbackOptions>? configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }

            // Make sure we signal that we've called AddSpaFallback.
            services.TryAddSingleton<SpaFallbackMarkerService>();

            // The StartupFilter is responsible for adding a marker middleware at the end of the pipeline.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, StartupFilter>());

            return services;
        }

        public static IApplicationBuilder UseSpaFallback(this IApplicationBuilder app)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var marker = app.ApplicationServices.GetService<SpaFallbackMarkerService>();

            if (marker is null)
            {
                var message = new StringBuilder()
                    .AppendLine($"Unable to find the required services for the {nameof(UseSpaFallback)} middleware to function correctly.")
                    .AppendLine($"Make sure you call {AddServices} before calling {UseMiddleware}.")
                    .AppendLine("This is typically done inside the ConfigureServices method in your Startup class.")
                    .ToString();

                throw new InvalidOperationException(message);
            }

            // Set the key to signal that the marker middleware should be added.
            app.Properties[MarkerKey] = true;

            return app.UseMiddleware<SpaFallbackMiddleware>();
        }

        internal static bool ShouldFallback(this HttpContext context, SpaFallbackOptions options)
        {
            if (context.Response.HasStarted)
            {
                return false;
            }

            if (context.Response.StatusCode != StatusCodes.Status404NotFound)
            {
                return false;
            }

            if (!IsGet(context.Request.Method))
            {
                return false;
            }

            // Fallback only on "hard" 404s, i.e. when the request reached the marker middleware.
            if (!context.Items.ContainsKey(MarkerKey))
            {
                return false;
            }

            if (HasFileExtension(context.Request.Path))
            {
                return options.AllowFileExtensions;
            }

            return true;
        }

        internal static bool ShouldThrow(this HttpContext context, SpaFallbackOptions options)
        {
            return context.Response.StatusCode == StatusCodes.Status404NotFound && options.ThrowIfFallbackFails;
        }

        private static bool HasFileExtension(this PathString path)
        {
            return path.HasValue && Path.HasExtension(path.Value);
        }

        private class StartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    next(app);

                    // We only want to add the end middleware if
                    // UseSpaFallback has been called on the builder.
                    if (app.Properties.ContainsKey(MarkerKey))
                    {
                        app.UseMiddleware<MarkerMiddleware>();
                    }
                };
            }

            private class MarkerMiddleware
            {
                public MarkerMiddleware(RequestDelegate next)
                {
                    Next = next;
                }

                private RequestDelegate Next { get; }

                public Task Invoke(HttpContext context)
                {
                    // This marker is used to signal that the request wasn't
                    // handled and reached the end of the application pipeline.
                    context.Items[MarkerKey] = true;
                    return Next(context);
                }
            }
        }

        /// <summary>
        /// A marker class used to determine if <see cref="AddSpaFallback(IServiceCollection)"/>
        /// has been called before calling <see cref="UseSpaFallback"/>.
        /// </summary>
        private class SpaFallbackMarkerService
        {
        }
    }
}
