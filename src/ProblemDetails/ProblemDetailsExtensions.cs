using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.ProblemDetails
{
    public static class ProblemDetailsExtensions
    {
        public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app)
        {
            return app.UseProblemDetails(configure: null);
        }

        public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app, Action<ProblemDetailsOptions> configure)
        {
            var options = new ProblemDetailsOptions();

            configure?.Invoke(options);

            ConfigureDefaults(options);

            return app.UseMiddleware<ProblemDetailsMiddleware>(Options.Create(options));
        }

        private static void ConfigureDefaults(ProblemDetailsOptions options)
        {
            if (options.IncludeExceptionDetails == null)
            {
                options.IncludeExceptionDetails = IncludeExceptionDetails;
            }
        }

        private static bool IncludeExceptionDetails(HttpContext context)
        {
            return context.RequestServices.GetRequiredService<IHostingEnvironment>().IsDevelopment();
        }
    }
}
