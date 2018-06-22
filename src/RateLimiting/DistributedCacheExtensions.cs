using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Hellang.Middleware.RateLimiting
{
    internal static class DistributedCacheExtensions
    {
        public static async Task<int> IncrementAsync(this IDistributedCache cache, string key, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            var countBytes = await cache.GetAsync(key, cancellationToken);

            var count = 0;

            if (countBytes != null)
            {
                count = BitConverter.ToInt32(countBytes, startIndex: 0);
            }

            countBytes = BitConverter.GetBytes(++count);

            await cache.SetAsync(key, countBytes, options, cancellationToken);

            return count;
        }
    }
}
