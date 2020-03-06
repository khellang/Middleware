using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#if NETCOREAPP3_0
using Microsoft.Extensions.Hosting;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#else
using Microsoft.AspNetCore.Hosting;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsOptionsSetup : IConfigureOptions<ProblemDetailsOptions>
    {
        public void Configure(ProblemDetailsOptions options)
        {
            if (options.IncludeExceptionDetails == null)
            {
                options.IncludeExceptionDetails = IncludeExceptionDetails;
            }

            if (options.ShouldLogUnhandledException == null)
            {
                options.ShouldLogUnhandledException = (ctx, e, d) => IsServerError(d.Status);
            }

            if (options.MapStatusCode == null)
            {
                options.MapStatusCode = (ctx, statusCode) => new StatusCodeProblemDetails(statusCode);
            }

            if (options.IsProblem == null)
            {
                options.IsProblem = IsProblem;
            }
        }

        private static bool IncludeExceptionDetails(HttpContext context)
        {
            return context.RequestServices.GetRequiredService<IHostingEnvironment>().IsDevelopment();
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
