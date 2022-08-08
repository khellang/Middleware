using System;
using System.Collections.Generic;
using System.Linq;
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

        protected ProblemDetailsOptions Options { get; }

        protected ILogger<ProblemDetailsFactory> Logger { get; }

        private ExceptionDetailsProvider DetailsProvider { get; }

        [Obsolete("Use " + nameof(CreateExceptionProblemDetails) + " instead.")]
        public MvcProblemDetails? GetDetails(HttpContext context, Exception error) => CreateExceptionProblemDetails(context, error);

        public virtual MvcProblemDetails? CreateExceptionProblemDetails(HttpContext context, Exception error)
        {
            MvcProblemDetails? result;
            if (error is ProblemDetailsException problem)
            {
                if (problem.InnerException is null)
                {
                    // The user has already provided a valid problem details object.
                    return problem.Details;
                }

                // Unwrap the inner exception to get the correct exception details below.
                error = problem.InnerException;
                result = problem.Details;
            }
            else
            {
                result = MapToProblemDetails(context, error);

                if (result is null)
                {
                    // Developer has explicitly ignored the problem.
                    return null;
                }
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

            MvcProblemDetails? MapToProblemDetails(HttpContext context, Exception error)
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

        public override MvcProblemDetails CreateProblemDetails(
            HttpContext context,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            var status = statusCode ?? context.Response.StatusCode;

            // It feels weird to mutate the response inside this method, but it's the
            // only way to pass the status code to MapStatusCode and it will be set
            // on the response when writing the problem details response later anyway.
            context.Response.StatusCode = status;

            var result = Options.MapStatusCode(context);

            SetProblemDefaults(result, status, title, type, detail, instance);

            return result;
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext context,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            var result = new ValidationProblemDetails(modelStateDictionary);

            SetProblemDefaults(result, statusCode ?? Options.ValidationProblemStatusCode, title, type, detail, instance);

            return result;
        }

        public ValidationProblemDetails CreateValidationProblemDetails(HttpContext context, SerializableError error)
            => CreateValidationProblemDetails(context, error, Options.ValidationProblemStatusCode);

        public virtual ValidationProblemDetails CreateValidationProblemDetails(HttpContext context, SerializableError error, int? statusCode)
        {
            var errors = GetValidationErrors(error);

            return CreateValidationProblemDetails(context, errors, statusCode);

            static IDictionary<string, string[]> GetValidationErrors(SerializableError error)
                => error.Where(x => x.Value is string[]).ToDictionary(x => x.Key, x => (string[])x.Value);
        }

        public virtual ValidationProblemDetails CreateValidationProblemDetails(HttpContext context, IDictionary<string, string[]> errors)
            => CreateValidationProblemDetails(context, errors, Options.ValidationProblemStatusCode);

        public virtual ValidationProblemDetails CreateValidationProblemDetails(HttpContext context, IDictionary<string, string[]> errors, int? statusCode)
        {
            var result = new ValidationProblemDetails(errors);

            SetProblemDefaults(result, statusCode ?? Options.ValidationProblemStatusCode);

            return result;
        }

        private static void SetProblemDefaults(
            MvcProblemDetails result,
            int statusCode,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            result.Status = statusCode;
            result.Title = title ?? result.Title;
            result.Type = type ?? result.Type ?? StatusCodeProblemDetails.GetDefaultType(statusCode);
            result.Detail = detail ?? result.Detail;
            result.Instance = instance ?? result.Instance;
        }
    }
}
