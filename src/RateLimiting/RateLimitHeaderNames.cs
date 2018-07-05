namespace Hellang.Middleware.RateLimiting
{
    public static class RateLimitHeaderNames
    {
        public const string XRateLimitLimit = "X-RateLimit-Limit";
        public const string XRateLimitReset = "X-RateLimit-Reset";
        public const string XRateLimitRemaining = "X-RateLimit-Remaining";
    }
}
