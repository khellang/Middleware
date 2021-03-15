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
                    result = result.WithExceptionDetails(Options.ExceptionDetailsPropertyName, error, DetailsProvider.GetDetails(error));
                }
                catch (Exception e)
                {
                    // Failed to get exception details for the specific exception.
                    // Just log the failure and return the original problem details below.
                    Logger.ProblemDetailsMiddlewareException(e);
                }
            }

            return AddDefaults(context, result);
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
            var status = statusCode ?? StatusCodes.Status500InternalServerError;

            var result = StatusCodeProblemDetails.Create(status);

            result.Title = title ?? result.Title;
            result.Type = type ?? result.Type ?? StatusCodeProblemDetails.GetDefaultType(status);
            result.Detail = detail ?? result.Detail;
            result.Instance = instance ?? result.Instance;

            return AddDefaults(context, result);
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

            return AddDefaults(context, result);
        }

        private TProblem AddDefaults<TProblem>(HttpContext context, TProblem result)
            where TProblem : MvcProblemDetails
        {
            Options.AddTraceId(context, result);

            Options.OnBeforeWriteDetails?.Invoke(context, result);

            return result;
        }
    }
}
