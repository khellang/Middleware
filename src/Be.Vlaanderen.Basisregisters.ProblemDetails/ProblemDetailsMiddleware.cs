namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;

    public class ProblemDetailsMiddleware
    {
        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        private static readonly RouteData EmptyRouteData = new RouteData();

        private RequestDelegate Next { get; }

        private ProblemDetailsOptions Options { get; }

        private IActionResultExecutor<ObjectResult> Executor { get; }

        private ILogger<ProblemDetailsMiddleware> Logger { get; }

        public ProblemDetailsMiddleware(
            RequestDelegate next,
            IOptions<ProblemDetailsOptions> options,
            IActionResultExecutor<ObjectResult> executor,
            ILogger<ProblemDetailsMiddleware> logger)
        {
            Next = next;
            Options = options.Value;
            Executor = executor;
            Logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await Next(context);

                if (Options.IsProblem != null && Options.IsProblem(context))
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
                if (context.Response.HasStarted)
                {
                    Logger.ResponseStarted();
                    throw; // Re-throw the original exception if we can't handle it properly.
                }

                try
                {
                    ClearResponse(context, StatusCodes.Status500InternalServerError);

                    var details = GetDetails(context, error);

                    if (Options.ShouldLogUnhandledException != null && Options.ShouldLogUnhandledException(context, error, details))
                        Logger.UnhandledException(error);

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

        private ProblemDetails GetDetails(HttpContext context, Exception? error)
        {
            var statusCode = context.Response.StatusCode;

            if (error == null)
                return Options.MapStatusCode!(context, statusCode);

            var result = GetProblemDetails(context, error);

            // We don't want to leak exception details,
            // even if the user mapped the exception into ExceptionProblemDetails.
            return result is ExceptionProblemDetails ex
                ? Options.MapStatusCode!(context, ex.HttpStatus ?? statusCode)
                : result;
        }

        private ProblemDetails GetProblemDetails(HttpContext context, Exception error)
        {
            // The user has already provided a valid problem details object.
            if (error is ProblemDetailsException problem)
                return problem.Details;

            // The user has set up a mapping for the specific exception type.
            if (Options.TryMapProblemDetails(context, error, out var result))
                return result;

            // Fall back to the generic exception problem details.
            return new ExceptionProblemDetails(error);
        }

        private Task WriteProblemDetails(HttpContext context, ProblemDetails details)
        {
            Options.OnBeforeWriteDetails?.Invoke(context, details);

            var routeData = context.GetRouteData() ?? EmptyRouteData;

            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            if (context.Request.Headers[HeaderNames.Accept].Contains("application/ld+json"))
            {
                context.Request.Headers[HeaderNames.Accept] = "application/json";
            }

            var result = new ObjectResult(details)
            {
                StatusCode = details.HttpStatus ?? context.Response.StatusCode,
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
                    headers.Add(header);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            foreach (var header in headers)
                context.Response.Headers.Add(header);
        }
    }
}
