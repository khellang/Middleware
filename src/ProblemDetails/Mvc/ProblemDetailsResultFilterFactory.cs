using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsResultFilterFactory : IFilterFactory, IOrderedFilter
    {
        public bool IsReusable => true;

        /// <summary>
        /// The same order as the built-in ClientErrorResultFilterFactory.
        /// </summary>
        public int Order => -2000;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<ProblemDetailsResultFilter>(serviceProvider);
        }
    }
}
