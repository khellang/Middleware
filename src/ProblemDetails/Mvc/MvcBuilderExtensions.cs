using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MvcProblemDetailsFactory = Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    public static class MvcBuilderExtensions
    {
        /// <summary>
        /// Adds conventions to turn off MVC's built-in <see cref="ApiBehaviorOptions.ClientErrorMapping"/>,
        /// adds a <see cref="ProducesErrorResponseTypeAttribute"/> to all actions with in controllers with an
        /// <see cref="ApiControllerAttribute"/> and a result filter that transforms <see cref="ObjectResult"/>
        /// containing a <see cref="string"/> to <see cref="ProblemDetails"/> responses.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddProblemDetailsConventions(this IMvcBuilder builder)
        {
            AddProblemDetailsConventions(builder.Services);

            return builder;
        }

        /// <summary>
        /// Adds conventions to turn off MVC's built-in <see cref="ApiBehaviorOptions.ClientErrorMapping"/>,
        /// adds a <see cref="ProducesErrorResponseTypeAttribute"/> to all actions with in controllers with an
        /// <see cref="ApiControllerAttribute"/> and a result filter that transforms <see cref="ObjectResult"/>
        /// containing a <see cref="string"/> to <see cref="ProblemDetails"/> responses.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddProblemDetailsConventions(this IServiceCollection services)
        {
            // Forward the MVC problem details factory registration to the factory used by the middleware.
            services.Replace(
                ServiceDescriptor.Singleton<MvcProblemDetailsFactory>(p => p.GetRequiredService<ProblemDetailsFactory>()));

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<ApiBehaviorOptions>, ProblemDetailsApiBehaviorOptionsSetup>());

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, ProblemDetailsApplicationModelProvider>());

            return services;
        }
    }
}
