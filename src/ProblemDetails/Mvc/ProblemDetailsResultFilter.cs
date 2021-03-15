using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static Hellang.Middleware.ProblemDetails.ProblemDetailsOptionsSetup;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using MvcProblemDetailsFactory = Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsResultFilter : IAlwaysRunResultFilter, IOrderedFilter
    {
        public ProblemDetailsResultFilter(MvcProblemDetailsFactory factory)
        {
            Factory = factory;
        }

        /// <summary>
        /// The same order as the built-in ClientErrorResultFilter.
        /// </summary>
        public int Order => -2000;

        private MvcProblemDetailsFactory Factory { get; }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            // Only handle ObjectResult for now.
            if (context.Result is not ObjectResult result)
            {
                return;
            }

            // Make sure the result should be treated as a problem.
            if (!IsProblemStatusCode(result.StatusCode))
            {
                return;
            }

            // Only handle the string case for now.
            if (result.Value is not string title)
            {
                return;
            }

            var problemDetails = Factory.CreateProblemDetails(context.HttpContext, result.StatusCode, title);

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
                ContentTypes = {
                    "application/problem+json",
                    "application/problem+xml",
                },
            };
        }

        void IResultFilter.OnResultExecuted(ResultExecutedContext context)
        {
            // Not needed.
        }
    }
}
