namespace Be.Vlaanderen.Basisregisters.BasicApiProblem.Tests.Helpers
{
    using System;
    using Microsoft.Extensions.Logging;

    public sealed class LogEntry
    {
        public LogLevel Type { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string? Message { get; set; }

        public string? Category { get; set; }

        public int EventId { get; set; }
    }
}
