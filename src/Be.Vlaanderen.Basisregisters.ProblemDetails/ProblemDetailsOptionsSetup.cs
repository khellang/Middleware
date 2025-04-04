namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public class ProblemDetailsOptionsSetup : IConfigureOptions<ProblemDetailsOptions>
    {
        public void Configure(ProblemDetailsOptions options)
        {
            if (options.ShouldLogUnhandledException == null)
                options.ShouldLogUnhandledException = (ctx, e, d) => IsServerError(d.HttpStatus);

            if (options.MapStatusCode == null)
                options.MapStatusCode = (_, statusCode) => new StatusCodeProblemDetails(statusCode);

            if (options.IsProblem == null)
                options.IsProblem = IsProblem;
        }

        // Err on the side of caution and treat missing status code as server error.
        private static bool IsServerError(int? statusCode)
            => !statusCode.HasValue || statusCode.Value >= 500;

        private static bool IsProblem(HttpContext context)
        {
            if (context.Response.StatusCode < 400)
                return false;

            if (context.Response.StatusCode >= 600)
                return false;

            if (context.Response.ContentLength.HasValue)
                return false;

            if (string.IsNullOrEmpty(context.Response.ContentType))
                return true;

            return false;
        }
    }
}
