using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Hellang.Middleware.RateLimiting
{
    public class RateLimitingMiddleware
    {
        public RateLimitingMiddleware(RequestDelegate next, IOptions<RateLimitingOptions> options)
        {
            Next = next;
            Options = options.Value;
        }

        private RequestDelegate Next { get; }
        
        private RateLimitingOptions Options { get; }

        public async Task Invoke(HttpContext context)
        {
            if (await Options.IsAllowed(context))
            {
                // TODO: Log as debug.
                await Next(context);
                return;
            }

            if (await Options.IsBlocked(context))
            {
                // TODO: Log as info.
                // TODO: Create extensibility point for customizing the response.
                await WriteBody(context, StatusCodes.Status403Forbidden, "Forbidden\n");
                return;
            }

            var limits = await Options.GetLimits(context);

            if (limits.HasValue)
            {
                var result = limits.Value;

                // TODO: Create extensiblity point for customizing the headers.
                context.Response.Headers[RateLimitHeaderNames.XRateLimitLimit] = result.Limit.ToString();
                context.Response.Headers[RateLimitHeaderNames.XRateLimitRemaining] = result.Remaining.ToString();
                context.Response.Headers[RateLimitHeaderNames.XRateLimitReset] = result.ExpirationTime.ToUnixTimeSeconds().ToString();

                context.Response.Headers[HeaderNames.RetryAfter] = ((int)result.ExpirationPeriod.TotalSeconds).ToString();

                if (result.Count > result.Limit)
                {
                    // TODO: Log as info.
                    // TODO: Create extensibility point for customizing the response.
                    await WriteBody(context, StatusCodes.Status429TooManyRequests, "Retry Later\n");
                    return;
                }
            }

            await Next(context);
        }

        private static Task WriteBody(HttpContext context, int statusCode, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain";

            context.Response.ContentLength = bytes.Length;
            return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
