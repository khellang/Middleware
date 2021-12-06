using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

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
                options.MapStatusCode = ctx => StatusCodeProblemDetails.Create(ctx.Response.StatusCode);
            }

            if (options.IsProblem is null)
            {
                options.IsProblem = IsProblem;
            }

            if (options.GetTraceId is null)
            {
                options.GetTraceId = ctx => Activity.Current?.Id ?? ctx.TraceIdentifier;
            }

            if (string.IsNullOrEmpty(options.TraceIdPropertyName))
            {
                options.TraceIdPropertyName = ProblemDetailsOptions.DefaultTraceIdPropertyName;
            }

            if (string.IsNullOrEmpty(options.ExceptionDetailsPropertyName))
            {
                options.ExceptionDetailsPropertyName = ProblemDetailsOptions.DefaultExceptionDetailsPropertyName;
            }

            if (options.AppendCacheHeaders is null)
            {
                options.AppendCacheHeaders = (_, headers) =>
                {
                    headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
                    headers[HeaderNames.Pragma] = "no-cache";
                    headers[HeaderNames.ETag] = default;
                    headers[HeaderNames.Expires] = "0";
                };
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
            if (!IsProblemStatusCode(context.Response.StatusCode))
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

        internal static bool IsProblemStatusCode(int? statusCode)
        {
            return statusCode switch
            {
                >= 600 => false,
                < 400 => false,
                null => false,
                _ => true
            };
        }
    }
}
