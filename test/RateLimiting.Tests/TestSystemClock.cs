using System;
using Microsoft.Extensions.Internal;

namespace RateLimiting.Tests
{
    public class TestSystemClock : ISystemClock
    {
        public TestSystemClock()
        {
            UtcNow = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset UtcNow { get; set; }

        public void Advance(TimeSpan timeSpan)
        {
            UtcNow = UtcNow.Add(timeSpan);
        }
    }
}
