using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.RateLimiting
{
    public class RateLimitingMiddleware
    {
        public RateLimitingMiddleware(RequestDelegate next, IDistributedCache cache, IOptions<RateLimitingOptions> options)
        {
            Next = next;
            Cache = cache;
            Options = options.Value;
        }

        private RequestDelegate Next { get; }
        
        private IDistributedCache Cache { get; }
        
        private RateLimitingOptions Options { get; }

        public async Task Invoke(HttpContext context)
        {
            if (await Options.IsAllowed(context))
            {
                await Next(context);
                return;
            }

            if (await Options.IsBlocked(context))
            {
                await WriteBody(context, StatusCodes.Status403Forbidden, "Forbidden\n");
                return;
            }

            var limit = await Options.GetLimit(context);

            if (limit.HasValue)
            {
                var result = limit.Value;

                context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = ((int)result.ExpirationTime.TotalSeconds).ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, result.Limit - result.Count).ToString();

                if (result.Count > result.Limit)
                {
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
