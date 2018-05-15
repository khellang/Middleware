using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsMiddleware
    {
        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        private static readonly RouteData EmptyRouteData = new RouteData();

        private static readonly HashSet<string> CorsHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.AccessControlAllowCredentials,
            HeaderNames.AccessControlAllowHeaders,
            HeaderNames.AccessControlAllowMethods,
            HeaderNames.AccessControlAllowOrigin,
            HeaderNames.AccessControlExposeHeaders,
            HeaderNames.AccessControlMaxAge,
        };

        public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger, IActionResultExecutor<ObjectResult> executor)
        {
            Next = next;
            Logger = logger;
            Executor = executor;
        }

        private RequestDelegate Next { get; }

        private ILogger<ProblemDetailsMiddleware> Logger { get; }

        private IActionResultExecutor<ObjectResult> Executor { get; }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await Next(context);

                if (context.Response.HasStarted)
                {
                    Logger.LogDebug("Response has already started.");
                    return;
                }

                if (IsProblem(context))
                {
                    var statusCode = context.Response.StatusCode;

                    ClearResponse(context);

                    await WriteProblemDetails(context, new StatusCodeProblemDetails(statusCode));
                }
            }
            catch (Exception error)
            {
                if (context.Response.HasStarted)
                {
                    Logger.LogDebug("Response has already started.");
                    throw;
                }

                try
                {
                    ClearResponse(context);

                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    
                    if (error is ProblemDetailsException problem)
                    {
                        await WriteProblemDetails(context, problem.Details);
                        return;
                    }

                    await WriteProblemDetails(context, new ExceptionProblemDetails(error));
                    return;
                }
                catch (Exception inner)
                {
                    Logger.LogWarning(inner, "Exception occurred while writing problem details response.");
                }

                throw;
            }
        }

        private Task WriteProblemDetails(HttpContext context, MvcProblemDetails details)
        {
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

        private static void ClearResponse(HttpContext context)
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
                if (CorsHeaderNames.Contains(header.Key))
                {
                    headers.Add(header);
                }
            }

            context.Response.Clear();

            foreach (var header in headers)
            {
                context.Response.Headers.Add(header);
            }
        }
    }
}
