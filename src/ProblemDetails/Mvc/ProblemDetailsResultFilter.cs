using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using static Hellang.Middleware.ProblemDetails.ProblemDetailsOptionsSetup;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using MvcProblemDetailsFactory = Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsResultFilter : IAlwaysRunResultFilter, IOrderedFilter
    {
        public ProblemDetailsResultFilter(MvcProblemDetailsFactory factory, IOptions<ProblemDetailsOptions> options)
        {
            Factory = factory;
            Options = options.Value;
        }

        /// <summary>
        /// The same order as the built-in ClientErrorResultFilter.
        /// </summary>
        public int Order => -2000;

        private MvcProblemDetailsFactory Factory { get; }

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
                ProblemDetailsFactory.AddDefaults(Options, context.HttpContext, problemDetails);
                return;
            }

            // Make sure the result should be treated as a problem.
            if (!IsProblemStatusCode(result.StatusCode))
            {
                return;
            }

            // Only handle the string case for now.
            if (result.Value is not string detail)
            {
                return;
            }

            problemDetails = Factory.CreateProblemDetails(context.HttpContext, result.StatusCode, detail: detail);

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
                ContentTypes = Options.ContentTypes,
            };
        }

        void IResultFilter.OnResultExecuted(ResultExecutedContext context)
        {
            // Not needed.
        }
    }
}
