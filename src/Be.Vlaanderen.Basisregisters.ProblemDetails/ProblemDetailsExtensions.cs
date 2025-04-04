namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using System;

    public static class ProblemDetailsExtensions
    {
        public static IServiceCollection AddProblemDetails(this IServiceCollection services)
            => services.AddProblemDetails(configure: null);

        public static IServiceCollection AddProblemDetails(this IServiceCollection services, Action<ProblemDetailsOptions>? configure)
        {
            if (configure != null)
                services.Configure(configure);

            services.TryAddSingleton<ProblemDetailsMarkerService, ProblemDetailsMarkerService>();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ProblemDetailsOptions>, ProblemDetailsOptionsSetup>());

            return services;
        }

        public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app)
        {
            var markerService = app.ApplicationServices.GetService<ProblemDetailsMarkerService>();

            if (markerService is null)
                throw new InvalidOperationException($"Please call {nameof(IServiceCollection)}.{nameof(AddProblemDetails)} in ConfigureServices before adding the middleware.");

            return app.UseMiddleware<ProblemDetailsMiddleware>();
        }

        /// <summary>
        /// A marker class used to determine if the required services were added
        /// to the <see cref="IServiceCollection"/> before the middleware is configured.
        /// </summary>
        private class ProblemDetailsMarkerService { }
    }
}
