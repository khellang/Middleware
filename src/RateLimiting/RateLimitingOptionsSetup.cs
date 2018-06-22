using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.RateLimiting
{
    public class RateLimitingOptionsSetup : IConfigureOptions<RateLimitingOptions>
    {
        public RateLimitingOptionsSetup(IDistributedCache cache)
        {
            Cache = cache;
        }

        private IDistributedCache Cache { get; }

        public void Configure(RateLimitingOptions options)
        {
            if (options.Clock == null)
            {
                options.Clock = new SystemClock();
            }

            if (string.IsNullOrEmpty(options.CachePrefix))
            {
                options.CachePrefix = RateLimitingOptions.DefaultCachePrefix;
            }

            if (options.Cache == null)
            {
                options.Cache = Cache;
            }
        }
    }
}
