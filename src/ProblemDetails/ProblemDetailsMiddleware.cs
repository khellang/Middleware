using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsMiddleware
    {
        private static readonly ActionDescriptor EmptyActionDescriptor = new();

        private static readonly RouteData EmptyRouteData = new();

        public ProblemDetailsMiddleware(
            RequestDelegate next,
            IOptions<ProblemDetailsOptions> options,
            ProblemDetailsFactory factory,
            IActionResultExecutor<ObjectResult> executor,
            ILogger<ProblemDetailsMiddleware> logger)
        {
            Next = next;
            Factory = factory;
            Options = options.Value;
            Executor = executor;
            Logger = logger;
        }

        private RequestDelegate Next { get; }

        private ProblemDetailsFactory Factory { get; }

        private ProblemDetailsOptions Options { get; }

        private IActionResultExecutor<ObjectResult> Executor { get; }

        private ILogger<ProblemDetailsMiddleware> Logger { get; }

        public async Task Invoke(HttpContext context)
        {
            ExceptionDispatchInfo edi = null;

            try
            {
                await Next(context);

                if (Options.IsProblem(context))
                {
                    await HandleProblem(context);
                }
            }
            catch (Exception ex)
            {
                edi = ExceptionDispatchInfo.Capture(ex);
            }

            if (edi != null)
            {
                await HandleException(context, edi);
            }
        }

        private Task HandleProblem(HttpContext context)
        {
            if (context.Response.HasStarted)
            {
                Logger.ResponseStarted();
                return Task.CompletedTask;
            }

            ClearResponse(context, context.Response.StatusCode);

            var details = Factory.CreateProblemDetails(context);

            return WriteProblemDetails(context, details);
        }

        private async Task HandleException(HttpContext context, ExceptionDispatchInfo edi)
        {
            if (context.Response.HasStarted)
            {
                Logger.ResponseStarted();
                edi.Throw(); // Re-throw the original exception if we can't handle it properly.
            }

            try
            {
                ClearResponse(context, StatusCodes.Status500InternalServerError);

                var error = edi.SourceException;

                var details = Factory.GetDetails(context, error);

                if (details != null) // Don't handle the exception if we can't or don't want to convert it to ProblemDetails
                {
                    if (Options.ShouldLogUnhandledException(context, error, details))
                    {
                        Logger.UnhandledException(error);
                    }

                    await WriteProblemDetails(context, details);

                    if (!Options.ShouldRethrowException(context, error))
                    {
                        return;
                    }
                }
                else
                {
                    Logger.IgnoredException(error);
                }
            }
            catch (Exception inner)
            {
                // If we fail to write a problem response, we log the exception and throw the original below.
                Logger.ProblemDetailsMiddlewareException(inner);
            }

            edi.Throw(); // Re-throw the original exception if we can't handle it properly or it's intended.
        }

        private async Task WriteProblemDetails(HttpContext context, MvcProblemDetails details)
        {
            var routeData = context.GetRouteData() ?? EmptyRouteData;

            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            var result = new ObjectResult(details)
            {
                StatusCode = details.Status ?? context.Response.StatusCode,
                ContentTypes = Options.ContentTypes,
            };

            await Executor.ExecuteAsync(actionContext, result);

            await context.Response.CompleteAsync();
        }

        private void ClearResponse(HttpContext context, int statusCode)
        {
            var headers = new HeaderDictionary();

            // Make sure problem responses are never cached.
            Options.AppendCacheHeaders(context, headers);

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
