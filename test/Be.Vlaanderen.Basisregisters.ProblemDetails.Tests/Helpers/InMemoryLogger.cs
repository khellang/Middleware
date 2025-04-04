namespace Be.Vlaanderen.Basisregisters.BasicApiProblem.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;

    public class InMemoryLogger<T> : ILogger<T>
    {
        private static readonly string? CategoryName = typeof(T).FullName;

        private readonly List<LogEntry> _messages = [];

        public IEnumerable<LogEntry> Messages
        {
            get
            {
                lock (_messages)
                    return _messages.ToList();
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = new LogEntry
            {
                Type = logLevel,
                Timestamp = DateTimeOffset.UtcNow,
                Message = formatter(state, exception),
                Category = CategoryName,
                EventId = eventId.Id,
            };

            lock (_messages)
                _messages.Add(message);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => throw new NotImplementedException();
    }
}
