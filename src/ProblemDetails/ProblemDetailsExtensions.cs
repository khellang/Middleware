using Microsoft.AspNetCore.Builder;

namespace Hellang.Middleware.ProblemDetails
{
    public static class ProblemDetailsExtensions
    {
        public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ProblemDetailsMiddleware>();
        }
    }
}
