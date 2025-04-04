namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.CodeAnalysis;

    internal static class LoggerExtensions
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly Action<ILogger, Exception> _unhandledException =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, "UnhandledException"), "An unhandled exception has occurred while executing the request.");

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly Action<ILogger, Exception?> _responseStarted =
            LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the problem details middleware will not be executed.");

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly Action<ILogger, Exception> _problemDetailsMiddlewareException =
            LoggerMessage.Define(LogLevel.Error, new EventId(3, "Exception"), "An exception was thrown attempting to execute the problem details middleware.");

        public static void UnhandledException(this ILogger logger, Exception exception) => _unhandledException(logger, exception);

        public static void ResponseStarted(this ILogger logger) => _responseStarted(logger, null);

        public static void ProblemDetailsMiddlewareException(this ILogger logger, Exception exception) => _problemDetailsMiddlewareException(logger, exception);
    }
}
