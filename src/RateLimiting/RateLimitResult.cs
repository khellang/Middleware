using System;

namespace Hellang.Middleware.RateLimiting
{
    public readonly struct RateLimitResult
    {
        public RateLimitResult(string discriminator, int limit, int count, TimeSpan expirationPeriod, DateTimeOffset expirationTime)
        {
            Discriminator = discriminator;
            Limit = limit;
            Count = count;
            ExpirationPeriod = expirationPeriod;
            ExpirationTime = expirationTime;
        }

        public string Discriminator { get; }

        public int Limit { get; }

        public int Count { get; }

        public int Remaining => Math.Max(0, Limit - Count);

        public TimeSpan ExpirationPeriod { get; }

        public DateTimeOffset ExpirationTime { get; }
    }
}
