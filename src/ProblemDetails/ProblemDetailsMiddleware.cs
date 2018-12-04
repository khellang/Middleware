﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.StackTrace.Sources;
using Microsoft.Net.Http.Headers;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsMiddleware
    {
        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        private static readonly RouteData EmptyRouteData = new RouteData();

        public ProblemDetailsMiddleware(
            RequestDelegate next,
            IOptions<ProblemDetailsOptions> options,
            IActionResultExecutor<ObjectResult> executor,
            IHostingEnvironment environment,
            ILogger<ProblemDetailsMiddleware> logger)
        {
            Next = next;
            Options = options.Value;
            Executor = executor;
            Logger = logger;
            var fileProvider = Options.FileProvider ?? environment.ContentRootFileProvider;
            DetailsProvider = new ExceptionDetailsProvider(fileProvider, Options.SourceCodeLineCount);
        }

        private RequestDelegate Next { get; }

        private ProblemDetailsOptions Options { get; }

        private IActionResultExecutor<ObjectResult> Executor { get; }

        private ILogger<ProblemDetailsMiddleware> Logger { get; }

        private ExceptionDetailsProvider DetailsProvider { get; }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await Next(context);

                if (Options.IsProblem(context))
                {
                    if (context.Response.HasStarted)
                    {
                        Logger.ResponseStarted();
                        return;
                    }
                    
                    ClearResponse(context, context.Response.StatusCode);

                    var details = GetDetails(context, error: null);

                    await WriteProblemDetails(context, details);
                }
            }
            catch (Exception error)
            {
                Logger.UnhandledException(error);

                if (context.Response.HasStarted)
                {
                    Logger.ResponseStarted();
                    throw; // Re-throw the original exception if we can't handle it properly.
                }

                try
                {
                    ClearResponse(context, StatusCodes.Status500InternalServerError);

                    var details = GetDetails(context, error);

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

        private MvcProblemDetails GetDetails(HttpContext context, Exception error)
        {
            var statusCode = context.Response.StatusCode;

            if (error == null)
            {
                return Options.MapStatusCode(statusCode);
            }

            var result = GetProblemDetails(error);

            // We don't want to leak exception details unless it's configured,
            // even if the user mapped the exception into ExceptionProblemDetails.
            if (result is ExceptionProblemDetails ex)
            {
                if (Options.IncludeExceptionDetails(context))
                {
                    try
                    {
                        var details = DetailsProvider.GetDetails(ex.Error);
                        return new DeveloperProblemDetails(ex, details);
                    }
                    catch (Exception e)
                    {
                        // Failed to get exception details for the specific exception.
                        // Fall back to generic status code problem details below.
                        Logger.ProblemDetailsMiddlewareException(e);
                    }
                }

                return Options.MapStatusCode(ex.Status ?? statusCode);
            }

            return result;
        }

        private MvcProblemDetails GetProblemDetails(Exception error)
        {
            if (error is ProblemDetailsException problem)
            {
                // The user has already provided a valid problem details object.
                return problem.Details;
            }

            if (Options.TryMapProblemDetails(error, out var result))
            {
                // The user has set up a mapping for the specific exception type.
                return result;
            }

            // Fall back to the generic exception problem details.
            return new ExceptionProblemDetails(error);
        }

        private Task WriteProblemDetails(HttpContext context, MvcProblemDetails details)
        {
            if (Options.OnBeforeWriteDetails != null)
            {
                Options.OnBeforeWriteDetails(details);
            }

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
                if (Options.AllowedHeaderNames.Contains(header.Key))
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
