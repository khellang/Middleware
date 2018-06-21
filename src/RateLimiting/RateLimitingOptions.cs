using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.RateLimiting
{
    public class RateLimitingOptions
    {
        public IRateLimitingStrategy Strategy { get; set; }
    }

    public class RateLimitingOptionsSetup : IPostConfigureOptions<RateLimitingOptions>
    {
        public void PostConfigure(string name, RateLimitingOptions options)
        {
            if (options.Strategy == null)
            {
                options.Strategy = new DefaultRateLimitingStrategy();
            }
        }
    }

    public interface IRateLimitingStrategy
    {
        string GetClientIdentifier(HttpContext context);
    }

    public class DefaultRateLimitingStrategy : IRateLimitingStrategy
    {
        public string GetClientIdentifier(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
