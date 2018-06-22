using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace Hellang.Middleware.RateLimiting
{
    internal class RateLimitDescriptor
    {
        public RateLimitDescriptor(string name, AsyncSelector<int> limit, AsyncSelector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            Name = name;
            Limit = limit;
            Period = period;
            Discriminator = discriminator;
        }

        public string Name { get; }

        private AsyncSelector<int> Limit { get; }

        private AsyncSelector<TimeSpan> Period { get; }

        private AsyncSelector<string> Discriminator { get; }

        public async Task<RateLimitResult?> GetLimit(HttpContext context, RateLimitingOptions options)
        {
            var discriminator = await Discriminator.Invoke(context);

            if (string.IsNullOrEmpty(discriminator))
            {
                return default;
            }

            var period = await Period.Invoke(context);
            var limit = await Limit.Invoke(context);

            var periodSeconds = (int)period.TotalSeconds;
            var nowSeconds = options.Clock.UtcNow.ToUnixTimeSeconds();

            var key = $"{options.CachePrefix}:{Name}:{discriminator}:{nowSeconds / periodSeconds}";
            var expirationTime = TimeSpan.FromSeconds(periodSeconds - (nowSeconds % periodSeconds));

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            };

            var count = await options.Cache.IncrementAsync(key, cacheOptions, context.RequestAborted);

            return new RateLimitResult(limit, count, expirationTime);
        }
    }
}
