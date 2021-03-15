using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<ApiBehaviorOptions>, ProblemDetailsApiBehaviorOptionsSetup>());

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, ProblemDetailsApplicationModelProvider>());

            return builder;
        }
    }
}
