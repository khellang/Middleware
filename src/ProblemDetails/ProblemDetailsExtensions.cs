using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using LibProblemDetailsFactory = Hellang.Middleware.ProblemDetails.ProblemDetailsFactory;

// TODO: Move to Microsoft.Extensions.DependencyInjection
namespace Hellang.Middleware.ProblemDetails
{
    public static class ProblemDetailsExtensions
    {
        /// <summary>
        /// Adds the required services for <see cref="UseProblemDetails"/> to work correctly,
        /// using the default options.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        public static IServiceCollection AddProblemDetails(this IServiceCollection services)
        {
            return services.AddProblemDetails(configure: null);
        }

        /// <summary>
        /// Adds the required services for <see cref="UseProblemDetails"/> to work correctly,
        /// using the specified <paramref name="configure"/> callback for configuration.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configure"></param>
        public static IServiceCollection AddProblemDetails(this IServiceCollection services, Action<ProblemDetailsOptions>? configure)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }

            services.TryAddSingleton<LibProblemDetailsFactory>();
            services.TryAddSingleton<ProblemDetailsMarkerService, ProblemDetailsMarkerService>();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ProblemDetailsOptions>, ProblemDetailsOptionsSetup>());

#if NET6_0_OR_GREATER
            services.TryAddSingleton<IActionResultExecutor<ObjectResult>, MinimalApiResultExecutor>();
#endif

            return services;
        }

        /// <summary>
        /// Adds the <see cref="ProblemDetailsMiddleware"/> to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder to add the middleware to.</param>
        /// <exception cref="InvalidOperationException">If <see cref="AddProblemDetails(IServiceCollection)"/> hasn't been called.</exception>
        public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app)
        {
            var markerService = app.ApplicationServices.GetService<ProblemDetailsMarkerService>();

            if (markerService is null)
            {
                throw new InvalidOperationException(
                    $"Please call {nameof(IServiceCollection)}.{nameof(AddProblemDetails)} in ConfigureServices before adding the middleware.");
            }

            return app.UseMiddleware<ProblemDetailsMiddleware>();
        }

        /// <summary>
        /// A marker class used to determine if the required services were added
        /// to the <see cref="IServiceCollection"/> before the middleware is configured.
        /// </summary>
        private class ProblemDetailsMarkerService
        {
        }
    }
}
