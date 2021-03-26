using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using static Hellang.Middleware.ProblemDetails.ProblemDetailsOptionsSetup;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsResultFilter : IAlwaysRunResultFilter, IOrderedFilter
    {
        public ProblemDetailsResultFilter(ProblemDetailsFactory factory, IOptions<ProblemDetailsOptions> options)
        {
            Factory = factory;
            Options = options.Value;
        }

        /// <summary>
        /// The same order as the built-in ClientErrorResultFilter.
        /// </summary>
        public int Order => -2000;

        private ProblemDetailsFactory Factory { get; }

        private ProblemDetailsOptions Options { get; }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            // Only handle ObjectResult for now.
            if (context.Result is not ObjectResult result)
            {
                return;
            }

            if (result.Value is MvcProblemDetails problemDetails)
            {
                // Add defaults, like trace ID, if user has supplied a ProblemDetails instance.
                Options.CallBeforeWriteHook(context.HttpContext, problemDetails);
                return;
            }

            // This is (most likely) a result of calling (some subclass of)
            // ObjectResult(ModelState) which indicates a validation error.
            if (result.Value is SerializableError error)
            {
                problemDetails = Factory.CreateValidationProblemDetails(context.HttpContext, error, result.StatusCode);
                context.Result = CreateResult(context, problemDetails);
                return;
            }

            // Make sure the result should be treated as a problem.
            if (!IsProblemStatusCode(result.StatusCode))
            {
                return;
            }

            // If the result is a string, we treat it as the "detail" property.
            if (result.Value is string detail)
            {
                problemDetails = Factory.CreateProblemDetails(context.HttpContext, result.StatusCode, detail: detail);
                context.Result = CreateResult(context, problemDetails);
                return;
            }

            // If the result is an exception, we treat it as if it's been thrown.
            if (result.Value is Exception exception)
            {
                // Set the response status code because it might be used for mapping inside the factory.
                context.HttpContext.Response.StatusCode = result.StatusCode ?? StatusCodes.Status500InternalServerError;

                var details = Factory.GetDetails(context.HttpContext, exception);

                // Devs can choose to ignore errors by returning null.
                if (details is null)
                {
                    return;
                }

                context.Result = CreateResult(context, details);
            }
        }

        void IResultFilter.OnResultExecuted(ResultExecutedContext context)
        {
            // Not needed.
        }

        private ObjectResult CreateResult(ActionContext context, MvcProblemDetails problemDetails)
        {
            Options.CallBeforeWriteHook(context.HttpContext, problemDetails);

            return new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
                ContentTypes = Options.ContentTypes,
            };
        }
    }
}
