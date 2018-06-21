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

        public Task Invoke(HttpContext context)
        {
            if (IsSafelisted(context))
            {
                return Next(context);
            }

            if (IsBlocklisted(context))
            {
                return Block(context);
            }

            if (IsThrottled(context))
            {
                return Throttle(context);
            }

            return Next(context);
        }

        private bool IsSafelisted(HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        private bool IsBlocklisted(HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        private Task Block(HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        private bool IsThrottled(HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        private Task Throttle(HttpContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
