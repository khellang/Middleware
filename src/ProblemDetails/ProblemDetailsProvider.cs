using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.StackTrace.Sources;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsProvider
    {
        private readonly ILogger<ProblemDetailsProvider> _logger;
        public ProblemDetailsOptions Options { get; }
        private ExceptionDetailsProvider _detailsProvider { get; }

        public ProblemDetailsProvider(
            IOptions<ProblemDetailsOptions> options,
            ILogger<ProblemDetailsProvider> logger,
            IHostingEnvironment environment)
        {
            _logger = logger;
            Options = options.Value;
            var fileProvider = Options.FileProvider ?? environment.ContentRootFileProvider;
            _detailsProvider = new ExceptionDetailsProvider(fileProvider, logger, Options.SourceCodeLineCount);
        }

        public MvcProblemDetails GetDetails(HttpContext context, Exception error)
        {
            var statusCode = context.Response.StatusCode;

            if (error == null)
            {
                return Options.MapStatusCode(context, statusCode);
            }

            var result = GetProblemDetails(context, error);

            // We don't want to leak exception details unless it's configured,
            // even if the user mapped the exception into ExceptionProblemDetails.
            if (result is ExceptionProblemDetails ex)
            {
                if (Options.IncludeExceptionDetails(context))
                {
                    try
                    {
                        var details = _detailsProvider.GetDetails(ex.Error);
                        return new DeveloperProblemDetails(ex, details);
                    }
                    catch (Exception e)
                    {
                        // Failed to get exception details for the specific exception.
                        // Fall back to generic status code problem details below.
                        _logger.ProblemDetailsMiddlewareException(e);
                    }
                }

                return Options.MapStatusCode(context, ex.Status ?? statusCode);
            }

            return result;
        }

        private MvcProblemDetails GetProblemDetails(HttpContext context, Exception error)
        {
            if (error is ProblemDetailsException problem)
            {
                // The user has already provided a valid problem details object.
                return problem.Details;
            }

            if (Options.TryMapProblemDetails(context, error, out var result))
            {
                // The user has set up a mapping for the specific exception type.
                return result;
            }

            // Fall back to the generic exception problem details.
            return new ExceptionProblemDetails(error);
        }
    }
}
