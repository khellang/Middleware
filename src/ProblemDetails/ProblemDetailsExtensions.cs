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

            if (options.MapStatusCode == null)
            {
                options.MapStatusCode = statusCode => new StatusCodeProblemDetails(statusCode);
            }

            if (options.IsProblem == null)
            {
                options.IsProblem = IsProblem;
            }

            options.TryMap<NotImplementedException>(ex =>
                new ExceptionProblemDetails(ex, StatusCodes.Status501NotImplemented));
        }

        private static bool IncludeExceptionDetails(HttpContext context)
        {
            return context.RequestServices.GetRequiredService<IHostingEnvironment>().IsDevelopment();
        }

        private static bool IsProblem(HttpContext context)
        {
            if (context.Response.StatusCode < 400)
            {
                return false;
            }

            if (context.Response.StatusCode >= 600)
            {
                return false;
            }

            if (context.Response.ContentLength.HasValue)
            {
                return false;
            }

            if (string.IsNullOrEmpty(context.Response.ContentType))
            {
                return true;
            }

            return false;
        }
    }
}
