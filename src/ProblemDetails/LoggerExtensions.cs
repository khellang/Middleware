using System;
using Microsoft.Extensions.Logging;

namespace Hellang.Middleware.ProblemDetails
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _unhandledException =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, "UnhandledException"), "An unhandled exception has occurred while executing the request.");

        private static readonly Action<ILogger, Exception> _responseStarted =
            LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the problem details middleware will not be executed.");

        private static readonly Action<ILogger, Exception> _problemDetailsMiddlewareException =
            LoggerMessage.Define(LogLevel.Error, new EventId(3, "Exception"), "An exception was thrown attempting to execute the problem details middleware.");

        private static readonly Action<ILogger, Exception> _ignoredException =
            LoggerMessage.Define(LogLevel.Information, new EventId(4, "IgnoredException"), "An exception has occurred while executing the request, but it was ignored by custom mapping rules.");

        public static void UnhandledException(this ILogger logger, Exception exception)
        {
            _unhandledException(logger, exception);
        }

        public static void ResponseStarted(this ILogger logger)
        {
            _responseStarted(logger, null);
        }

        public static void ProblemDetailsMiddlewareException(this ILogger logger, Exception exception)
        {
            _problemDetailsMiddlewareException(logger, exception);
        }

        public static void IgnoredException(this ILogger logger, Exception exception)
        {
            _ignoredException(logger, exception);
        }
    }
}
