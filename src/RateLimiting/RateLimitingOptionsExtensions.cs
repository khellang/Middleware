using System;
using System.Net;

namespace Hellang.Middleware.RateLimiting
{
    public static class RateLimitingOptionsExtensions
    {
        public static RateLimitingOptions Allow(this RateLimitingOptions options, string name, IPAddress address)
        {
            return options.Allow(name, ctx => ctx.Connection.RemoteIpAddress.Equals(address));
        }

        public static RateLimitingOptions Allow(this RateLimitingOptions options, string name, IPNetwork network)
        {
            return options.Allow(name, ctx => network.Contains(ctx.Connection.RemoteIpAddress));
        }

        public static RateLimitingOptions Allow(this RateLimitingOptions options, string name, Selector<bool> predicate)
        {
            return options.Allow(name, predicate.ToAsync());
        }

        public static RateLimitingOptions Block(this RateLimitingOptions options, string name, IPAddress address)
        {
            return options.Block(name, ctx => ctx.Connection.RemoteIpAddress.Equals(address));
        }

        public static RateLimitingOptions Block(this RateLimitingOptions options, string name, IPNetwork network)
        {
            return options.Block(name, ctx => network.Contains(ctx.Connection.RemoteIpAddress));
        }

        public static RateLimitingOptions Block(this RateLimitingOptions options, string name, Selector<bool> predicate)
        {
            return options.Block(name, predicate.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, Selector<int> limit, AsyncSelector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, limit.ToAsync(), period, discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, AsyncSelector<int> limit, Selector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, limit, period.ToAsync(), discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, AsyncSelector<int> limit, AsyncSelector<TimeSpan> period, Selector<string> discriminator)
        {
            return options.Limit(name, limit, period, discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, Selector<int> limit, Selector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, limit.ToAsync(), period.ToAsync(), discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, AsyncSelector<int> limit, Selector<TimeSpan> period, Selector<string> discriminator)
        {
            return options.Limit(name, limit, period.ToAsync(), discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, Selector<int> limit, Selector<TimeSpan> period, Selector<string> discriminator)
        {
            return options.Limit(name, limit.ToAsync(), period.ToAsync(), discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, int limit, Selector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, limit, period.ToAsync(), discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, int limit, AsyncSelector<TimeSpan> period, Selector<string> discriminator)
        {
            return options.Limit(name, limit, period, discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, int limit, Selector<TimeSpan> period, Selector<string> discriminator)
        {
            return options.Limit(name, limit, period.ToAsync(), discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, int limit, AsyncSelector<TimeSpan> period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, _ => limit, period, discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, Selector<int> limit, TimeSpan period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, limit.ToAsync(), period, discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, AsyncSelector<int> limit, TimeSpan period, Selector<string> discriminator)
        {
            return options.Limit(name, limit, period, discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, Selector<int> limit, TimeSpan period, Selector<string> discriminator)
        {
            return options.Limit(name, limit.ToAsync(), period, discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, AsyncSelector<int> limit, TimeSpan period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, limit, _ => period, discriminator);
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, int limit, TimeSpan period, Selector<string> discriminator)
        {
            return options.Limit(name, limit, period, discriminator.ToAsync());
        }

        public static RateLimitingOptions Limit(this RateLimitingOptions options, string name, int limit, TimeSpan period, AsyncSelector<string> discriminator)
        {
            return options.Limit(name, _ => limit, _ => period, discriminator);
        }
    }
}