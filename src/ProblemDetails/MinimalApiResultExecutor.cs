#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Threading.Tasks;

namespace Hellang.Middleware.ProblemDetails
{
    internal class MinimalApiResultExecutor : IActionResultExecutor<ObjectResult>
    {
        public Task ExecuteAsync(ActionContext context, ObjectResult result) =>
            Results.Json(result.Value, options: null, "application/problem+json", result.StatusCode)
                .ExecuteAsync(context.HttpContext);
    }
}
#endif
