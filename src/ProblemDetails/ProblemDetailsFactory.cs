using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.StackTrace.Sources;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsFactory
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

            if (Options.IncludeExceptionDetails(context, error))
            {
                try
                {
                    // Instead of returning a new object, we mutate the existing problem so users keep all details.
                    return result.WithExceptionDetails(error, DetailsProvider.GetDetails(error));
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
    }
}
