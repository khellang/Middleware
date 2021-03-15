using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
    {
        public void Configure(ApiBehaviorOptions options)
        {
            // Turn off MVC's built-in client error mapping to use the middleware instead.
            options.SuppressMapClientErrors = true;
        }
    }
}
