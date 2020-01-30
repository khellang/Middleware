using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsMiddleware
    {
        private readonly ProblemDetailsProvider _problemDetailsProvider;
        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        private static readonly RouteData EmptyRouteData = new RouteData();

        public ProblemDetailsMiddleware(
            RequestDelegate next,
            IActionResultExecutor<ObjectResult> executor,
            ILogger<ProblemDetailsMiddleware> logger,
            ProblemDetailsProvider problemDetailsProvider)
        {
            _problemDetailsProvider = problemDetailsProvider;
            Next = next;
            Executor = executor;
            Logger = logger;
        }

        private RequestDelegate Next { get; }
        private IActionResultExecutor<ObjectResult> Executor { get; }
        private ILogger<ProblemDetailsMiddleware> Logger { get; }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await Next(context);

                if (_problemDetailsProvider.Options.IsProblem(context))
                {
                    if (context.Response.HasStarted)
                    {
                        Logger.ResponseStarted();
                        return;
                    }

                    ClearResponse(context, context.Response.StatusCode);

                    var details = _problemDetailsProvider.GetDetails(context, error: null);

                    await WriteProblemDetails(context, details);
                }
            }
            catch (Exception error)
            {
                if (context.Response.HasStarted)
                {
                    Logger.ResponseStarted();
                    throw; // Re-throw the original exception if we can't handle it properly.
                }

                try
                {
                    ClearResponse(context, StatusCodes.Status500InternalServerError);

                    var details = _problemDetailsProvider.GetDetails(context, error);

                    if (_problemDetailsProvider.Options.ShouldLogUnhandledException(context, error, details))
                    {
                        Logger.UnhandledException(error);
                    }

                    await WriteProblemDetails(context, details);
                    return;
                }
                catch (Exception inner)
                {
                    // If we fail to write a problem response, we log the exception and throw the original below.
                    Logger.ProblemDetailsMiddlewareException(inner);
                }

                throw; // Re-throw the original exception if we can't handle it properly.
            }
        }

        private Task WriteProblemDetails(HttpContext context, MvcProblemDetails details)
        {
            _problemDetailsProvider.Options.OnBeforeWriteDetails?.Invoke(context, details);

            var routeData = context.GetRouteData() ?? EmptyRouteData;

            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            var result = new ObjectResult(details)
            {
                StatusCode = details.Status ?? context.Response.StatusCode,
                DeclaredType = details.GetType(),
            };

            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");

            return Executor.ExecuteAsync(actionContext, result);
        }

        private void ClearResponse(HttpContext context, int statusCode)
        {
            var headers = new HeaderDictionary();

            // Make sure problem responses are never cached.
            headers.Append(HeaderNames.CacheControl, "no-cache, no-store, must-revalidate");
            headers.Append(HeaderNames.Pragma, "no-cache");
            headers.Append(HeaderNames.Expires, "0");

            foreach (var header in context.Response.Headers)
            {
                // Because the CORS middleware adds all the headers early in the pipeline,
                // we want to copy over the existing Access-Control-* headers after resetting the response.
                if (_problemDetailsProvider.Options.AllowedHeaderNames.Contains(header.Key))
                {
                    headers.Add(header);
                }
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            foreach (var header in headers)
            {
                context.Response.Headers.Add(header);
            }
        }
    }
}
