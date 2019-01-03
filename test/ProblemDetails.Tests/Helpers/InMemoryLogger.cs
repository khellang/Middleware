﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Hellang.Middleware.ProblemDetails.Tests
{
    public class InMemoryLogger<T> : ILogger<T>
    {
        private readonly string _categoryName;

        private readonly List<LogEntry> _messages = new List<LogEntry>();

        public List<LogEntry> Messages
        {
            get { lock (_messages) return _messages.ToList(); }
        }

        public InMemoryLogger()
        {
            _categoryName = typeof(T).FullName;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = new LogEntry
            {
                Type = logLevel,
                Timestamp = DateTimeOffset.UtcNow,
                Message = formatter(state, exception),
                Category = _categoryName,
                EventId = eventId.Id,
            };

            lock (_messages)
                _messages.Add(message);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
    }
}