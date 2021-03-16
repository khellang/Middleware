using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.StackTrace.Sources;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using MvcProblemDetailsFactory = Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsFactory : MvcProblemDetailsFactory
    {
        public ProblemDetailsFactory(
            IOptions<ProblemDetailsOptions> options,
            ILogger<ProblemDetailsFactory> logger,
            IHostEnvironment environment)
        {
            Logger = logger;
            Options = options.Value;
            var fileProvider = Options.FileProvider ?? environment.ContentRootFileProvider;
            DetailsProvider = new ExceptionDetailsProvider(fileProvider, logger, Options.SourceCodeLineCount);
        }

        private ProblemDetailsOptions Options { get; }

        private ILogger<ProblemDetailsFactory> Logger { get; }

        private ExceptionDetailsProvider DetailsProvider { get; }

        public MvcProblemDetails GetDetails(HttpContext context, Exception error)
        {
            var result = CreateErrorProblemDetails(context, error);

            AddDefaults(Options, context, result);

            return result;
        }

        private MvcProblemDetails CreateErrorProblemDetails(HttpContext context, Exception error)
        {
            if (error is ProblemDetailsException problem)
            {
                // The user has already provided a valid problem details object.
                return problem.Details;
            }

            var result = MapToProblemDetails(context, error);

            if (result is null)
            {
                // Developer has explicitly ignored the problem.
                return null;
            }

            if (Options.IncludeExceptionDetails(context, error))
            {
                try
                {
                    // Instead of returning a new object, we mutate the existing problem so users keep all details.
                    return result.WithExceptionDetails(Options.ExceptionDetailsPropertyName, error, DetailsProvider.GetDetails(error));
                }
                catch (Exception e)
                {
                    // Failed to get exception details for the specific exception.
                    // Just log the failure and return the original problem details below.
                    Logger.ProblemDetailsMiddlewareException(e);
                }
            }

            return result;
        }

        private MvcProblemDetails MapToProblemDetails(HttpContext context, Exception error)
        {
            if (Options.TryMapProblemDetails(context, error, out var result))
            {
                // The user has set up a mapping for the specific exception type.
                return result;
            }

            // Fall back to the generic status code problem details.
            return Options.MapStatusCode(context);
        }

        public override MvcProblemDetails CreateProblemDetails(
            HttpContext context,
            int? statusCode = null,
            string title = null,
            string type = null,
            string detail = null,
            string instance = null)
        {
            var status = statusCode ?? context.Response.StatusCode;

            // It feels weird to mutate the response inside this method, but it's the
            // only way to pass the status code to MapStatusCode and it will be set
            // on the response when writing the problem details response later anyway.
            context.Response.StatusCode = status;

            var result = Options.MapStatusCode(context);

            result.Title = title ?? result.Title;
            result.Type = type ?? result.Type ?? StatusCodeProblemDetails.GetDefaultType(status);
            result.Detail = detail ?? result.Detail;
            result.Instance = instance ?? result.Instance;

            AddDefaults(Options, context, result);

            return result;
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext context,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string title = null,
            string type = null,
            string detail = null,
            string instance = null)
        {
            var status = statusCode ?? StatusCodes.Status422UnprocessableEntity;

            var result = new ValidationProblemDetails(modelStateDictionary)
            {
                Status = status,
            };

            result.Title = title ?? result.Title;
            result.Type = type ?? result.Type ?? StatusCodeProblemDetails.GetDefaultType(status);
            result.Detail = detail ?? result.Detail;
            result.Instance = instance ?? result.Instance;

            AddDefaults(Options, context, result);

            return result;
        }

        internal static void AddDefaults<TProblem>(ProblemDetailsOptions options, HttpContext context, TProblem result)
            where TProblem : MvcProblemDetails
        {
            options.AddTraceId(context, result);

            options.OnBeforeWriteDetails?.Invoke(context, result);
        }
    }
}
