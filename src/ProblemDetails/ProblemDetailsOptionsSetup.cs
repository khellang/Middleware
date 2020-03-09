using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsOptionsSetup : IConfigureOptions<ProblemDetailsOptions>
    {
        public void Configure(ProblemDetailsOptions options)
        {
            if (options.IncludeExceptionDetails is null)
            {
                options.IncludeExceptionDetails = IncludeExceptionDetails;
            }

            if (options.ShouldLogUnhandledException is null)
            {
                options.ShouldLogUnhandledException = (ctx, e, d) => IsServerError(d.Status);
            }

            if (options.MapStatusCode is null)
            {
                options.MapStatusCode = ctx => new StatusCodeProblemDetails(ctx.Response.StatusCode);
            }

            if (options.IsProblem is null)
            {
                options.IsProblem = IsProblem;
            }

            if (options.GetTraceId is null)
            {
                options.GetTraceId = ctx => Activity.Current?.Id ?? ctx.TraceIdentifier;
            }

            if (options.ContentTypes.Count == 0)
            {
                options.ContentTypes.Add("application/problem+json");
                options.ContentTypes.Add("application/problem+xml");
            }
        }

        private static bool IncludeExceptionDetails(HttpContext context, Exception exception)
        {
            return context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        }

        private static bool IsServerError(int? statusCode)
        {
            // Err on the side of caution and treat missing status code as server error.
            return !statusCode.HasValue || statusCode.Value >= 500;
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
