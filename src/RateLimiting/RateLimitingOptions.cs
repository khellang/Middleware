using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;

namespace Hellang.Middleware.RateLimiting
{
    public class RateLimitingOptions
    {
        public static readonly string DefaultCachePrefix = "rate-limit";

        public ISystemClock Clock { get; set; }

        public IDistributedCache Cache { get; set; }

        public string CachePrefix { get; set; } = DefaultCachePrefix;

        private List<Check> Allowed { get; } = new List<Check>();

        private List<Check> Blocked { get; } = new List<Check>();

        private List<RateLimitDescriptor> Limited { get; } = new List<RateLimitDescriptor>();

        public RateLimitingOptions Allow(string name, AsyncSelector<bool> predicate)
        {
            Allowed.Add(new Check(name, predicate));
            return this;
        }
        
        public RateLimitingOptions Block(string name, AsyncSelector<bool> predicate)
        {
            Blocked.Add(new Check(name, predicate));
            return this;
        }

        public RateLimitingOptions Limit(string name, AsyncSelector<int> limit, AsyncSelector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            Limited.Add(new RateLimitDescriptor(name, limit, period, discriminator));
            return this;
        }

        internal ValueTask<bool> IsAllowed(HttpContext context)
        {
            return HasMatch(Allowed, context);
        }

        internal ValueTask<bool> IsBlocked(HttpContext context)
        {
            return HasMatch(Blocked, context);
        }

        internal async ValueTask<RateLimitResult?> GetLimit(HttpContext context)
        {
            foreach (var throttle in Limited)
            {
                var limit = await throttle.GetLimit(context, this);

                if (limit.HasValue)
                {
                    return limit.Value;
                }
            }

            return default;
        }

        private static async ValueTask<bool> HasMatch(IEnumerable<Check> checks, HttpContext context)
        {
            foreach (var check in checks)
            {
                if (await check.Matches(context))
                {
                    return true;
                }
            }

            return false;
        }

        private class Check
        {
            public Check(string name, AsyncSelector<bool> predicate)
            {
                Name = name;
                Matches = predicate;
            }

            public string Name { get; }

            public AsyncSelector<bool> Matches { get; }
        }
    }
}
