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

        public async Task<RateLimitResult?> GetLimits(HttpContext context, RateLimitingOptions options)
        {
            var discriminator = await Discriminator.Invoke(context);

            if (string.IsNullOrEmpty(discriminator))
            {
                // TODO: Log missing discriminator as warning.
                return default;
            }

            var period = await Period.Invoke(context);
            var limit = await Limit.Invoke(context);

            var now = options.Clock.UtcNow;
            var periodSeconds = (int)period.TotalSeconds;
            var nowUnixSeconds = now.ToUnixTimeSeconds();

            var key = $"{options.CachePrefix}:{Name}:{discriminator}:{nowUnixSeconds / periodSeconds}";
            var expirationPeriod = TimeSpan.FromSeconds(periodSeconds - (nowUnixSeconds % periodSeconds));
            var expirationTime = now.Add(expirationPeriod);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expirationTime
            };

            // TODO: Replace with atomic operation - https://github.com/aspnet/Caching/issues/411
            var count = await options.Cache.IncrementAsync(key, cacheOptions, context.RequestAborted);

            return new RateLimitResult(discriminator, limit, count, expirationPeriod, expirationTime);
        }
    }
}
