using System;

namespace Hellang.Middleware.RateLimiting
{
    public readonly struct RateLimitResult
    {
        public RateLimitResult(int limit, int count, TimeSpan expirationTime)
        {
            Limit = limit;
            Count = count;
            ExpirationTime = expirationTime;
        }

        public int Limit { get; }

        public int Count { get; }

        public TimeSpan ExpirationTime { get; }
    }
}
