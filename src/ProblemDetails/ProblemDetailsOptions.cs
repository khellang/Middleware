using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsOptions
    {
        public const string DefaultExceptionDetailsPropertyName = "exceptionDetails";
        public const string DefaultTraceIdPropertyName = "traceId";

        public ProblemDetailsOptions()
        {
            SourceCodeLineCount = 6;
            Mappers = new List<ExceptionMapper>();
            RethrowPolicies = new List<Func<HttpContext, Exception, bool>>();
            ContentTypes = new MediaTypeCollection();
            TraceIdPropertyName = DefaultTraceIdPropertyName;
            ExceptionDetailsPropertyName = DefaultExceptionDetailsPropertyName;
            ValidationProblemStatusCode = StatusCodes.Status422UnprocessableEntity;
            AllowedHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                HeaderNames.AccessControlAllowCredentials,
                HeaderNames.AccessControlAllowHeaders,
                HeaderNames.AccessControlAllowMethods,
                HeaderNames.AccessControlAllowOrigin,
                HeaderNames.AccessControlExposeHeaders,
                HeaderNames.AccessControlMaxAge,

                HeaderNames.StrictTransportSecurity,

                HeaderNames.WWWAuthenticate,
            };
        }

        /// <summary>
        /// Gets or sets the number of source code lines to include for context in exception details.
        /// The default is <c>6</c>.
        /// </summary>
        public int SourceCodeLineCount { get; set; }

        /// <summary>
        /// The <see cref="IFileProvider"/> for getting file information when reading stack trace information.
        /// </summary>
        public IFileProvider FileProvider { get; set; } = null!;

        /// <summary>
        /// Gets or sets the function for getting a <c>traceId</c> to include in the problem response.
        /// The default gets the ID from <see cref="Activity.Current"/> with a
        /// fallback to <see cref="HttpContext.TraceIdentifier"/>.
        /// </summary>
        public Func<HttpContext, string?> GetTraceId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the predicate used for determining whether exception details (stack trace etc.)
        /// should be included in the problem details response.
        /// The default returns <c>true</c> when <see cref="IHostEnvironment.EnvironmentName"/> is "Development".
        /// </summary>
        public Func<HttpContext, Exception, bool> IncludeExceptionDetails { get; set; } = null!;

        /// <summary>
        /// The property name to use for traceId
        /// This defaults to <see cref="DefaultTraceIdPropertyName"/> (<c>traceId</c>).
        /// </summary>
        public string TraceIdPropertyName { get; set; }

        /// <summary>
        /// The property name to use for exception details.
        /// This defaults to <see cref="DefaultExceptionDetailsPropertyName"/> (<c>exceptionDetails</c>).
        /// </summary>
        public string ExceptionDetailsPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the predicate used for determining whether a request/response should be considered
        /// a problem or not. The default returns <c>true</c> if the following is true:
        /// <list type="bullet">
        ///   <item>
        ///     <description>The status code is between 400 and 600.</description>
        ///   </item>
        ///   <item>
        ///     <description>The <c>Content-Length</c> header is empty.</description>
        ///   </item>
        ///   <item>
        ///     <description>The <c>Content-Type</c> header is empty.</description>
        ///   </item>
        /// </list>
        /// </summary>
        public Func<HttpContext, bool> IsProblem { get; set; } = null!;

        /// <summary>
        /// Gets or sets the function for mapping response status codes to problem details instances.
        /// The default will just create a <see cref="StatusCodeProblemDetails"/> using the response
        /// status code of the current <see cref="HttpContext"/>.
        /// </summary>
        public Func<HttpContext, MvcProblemDetails> MapStatusCode { get; set; } = null!;

        /// <summary>
        /// Gets or sets a callback used to transform a problem details instance right before
        /// it is written to the response.
        /// </summary>
        public Action<HttpContext, MvcProblemDetails>? OnBeforeWriteDetails { get; set; }

        /// <summary>
        /// Gets or sets a predicate used for determining whether an exception should be logged as unhandled.
        /// The default returns <c>true</c> if the response status code doesn't have a value, or the
        /// value is <see cref="StatusCodes.Status500InternalServerError"/> or higher.
        /// </summary>
        public Func<HttpContext, Exception, MvcProblemDetails, bool> ShouldLogUnhandledException { get; set; } = null!;

        /// <summary>
        /// Gets or sets an action to populate response cache headers to prevent caching problem details responses.
        /// </summary>
        public Action<HttpContext, HeaderDictionary> AppendCacheHeaders { get; set; } = null!;

        /// <summary>
        /// Gets the set of headers that shouldn't be cleared when producing a problem details response.
        /// This includes CORS, HSTS and authentication challenge headers by default.
        /// </summary>
        public HashSet<string> AllowedHeaderNames { get; }

        /// <summary>
        /// Gets the supported <c>Content-Type</c> values for use in content negotiation.
        /// The default values are <c>application/problem+json</c> and <c>application/problem+xml</c>.
        /// </summary>
        public MediaTypeCollection ContentTypes { get; }

        /// <summary>
        /// Gets or sets the status code used for validation errors when using the MVC conventions.
        /// </summary>
        public int ValidationProblemStatusCode { get; set; }

        private List<ExceptionMapper> Mappers { get; }

        private List<Func<HttpContext, Exception, bool>> RethrowPolicies { get; }

        /// <summary>
        /// Maps the specified exception type <typeparamref name="TException"/> to the specified
        /// status code <paramref name="statusCode"/>. This also includes default values for
        /// <see cref="MvcProblemDetails.Type"/> and <see cref="MvcProblemDetails.Title"/>.
        /// </summary>
        /// <param name="statusCode">The status code to return for the specified exception.</param>
        /// <typeparam name="TException">The exception type to map to the specified status code.</typeparam>
        public void MapToStatusCode<TException>(int statusCode) where TException : Exception
        {
            Map<TException>((_, _) => StatusCodeProblemDetails.Create(statusCode));
        }

        /// <summary>
        /// Configures the middleware to ignore any exception of the specified exception type <typeparamref name="TException"/>.
        /// This will cause the exception to be rethrown to be handled upstream.
        /// </summary>
        /// <typeparam name="TException">The exception type to ignore.</typeparam>
        public void Ignore<TException>() where TException : Exception
        {
            Map<TException>((_, _) => null);
        }

        /// <summary>
        /// Configures the middleware to ignore exceptions of the specified exception type <typeparamref name="TException"/> that match the specified <paramref name="predicate"/>.
        /// This will cause the exception to be rethrown to be handled upstream.
        /// </summary>
        /// <param name="predicate">The predicate to check whether the exception should be ignored or not.</param>
        /// <typeparam name="TException">The exception type to ignore.</typeparam>
        public void Ignore<TException>(Func<HttpContext, TException, bool> predicate) where TException : Exception
        {
            Map(predicate, (_, _) => null);
        }

        /// <summary>
        /// Maps the specified exception type <typeparamref name="TException"/> to a <see cref="MvcProblemDetails"/> instance
        /// using the specified <paramref name="mapping"/> function.
        /// </summary>
        /// <remarks>
        /// Mappers are called in the order they're registered.
        /// Returning <c>null</c> from the mapper will signify that you can't or don't want to map the exception to <see cref="MvcProblemDetails"/>.
        /// This will cause the exception to be rethrown.
        /// </remarks>
        /// <param name="mapping">The mapping function for creating a problem details instance.</param>
        /// <typeparam name="TException">The exception type to map using the specified mapping function.</typeparam>
        public void Map<TException>(Func<TException, MvcProblemDetails?> mapping) where TException : Exception
        {
            Map<TException>((_, ex) => mapping(ex));
        }

        /// <summary>
        /// Maps the specified exception type <typeparamref name="TException"/> to a <see cref="MvcProblemDetails"/> instance
        /// using the specified <paramref name="mapping"/> function.
        /// </summary>
        /// <remarks>
        /// Mappers are called in the order they're registered.
        /// Returning <c>null</c> from the mapper will signify that you can't or don't want to map the exception to <see cref="MvcProblemDetails"/>.
        /// This will cause the exception to be rethrown.
        /// </remarks>
        /// <param name="mapping">The mapping function for creating a problem details instance.</param>
        /// <typeparam name="TException">The exception type to map using the specified mapping function.</typeparam>
        public void Map<TException>(Func<HttpContext, TException, MvcProblemDetails?> mapping) where TException : Exception
        {
            Map((_, _) => true, mapping);
        }

        /// <summary>
        /// Maps the specified exception type <typeparamref name="TException"/> to a <see cref="MvcProblemDetails"/> instance
        /// using the specified <paramref name="mapping"/> function.
        /// </summary>
        /// <remarks>
        /// Mappers are called in the order they're registered.
        /// Returning <c>null</c> from the mapper will signify that you can't or don't want to map the exception to <see cref="MvcProblemDetails"/>.
        /// This will cause the exception to be rethrown.
        /// </remarks>
        /// <param name="predicate">This Map will skip this exception if the predicate returns false.</param>
        /// <param name="mapping">The mapping function for creating a problem details instance.</param>
        /// <typeparam name="TException">The exception type to map using the specified mapping function.</typeparam>
        public void Map<TException>(
            Func<HttpContext, TException, bool> predicate,
            Func<HttpContext, TException, MvcProblemDetails?> mapping)
            where TException : Exception
        {
            Mappers.Add(new ExceptionMapper(
                typeof(TException),
                (ctx, ex) => mapping(ctx, (TException)ex),
                (ctx, ex) => predicate(ctx, (TException)ex)));
        }

        /// <summary>
        /// Marks the specified exception type <typeparamref name="TException"/> for re-throwing.
        /// This is useful if you have other upstream middleware that wants to handle the exception.
        /// </summary>
        /// <typeparam name="TException">The type of exception to re-throw.</typeparam>
        public void Rethrow<TException>() where TException : Exception
        {
            Rethrow<TException>((_, _) => true);
        }

        /// <summary>
        /// Marks the specified exception type <typeparamref name="TException"/> for re-throwing, using
        /// the specified <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to determine whether an exception should be re-thrown.</param>
        /// <typeparam name="TException">The type of exception to re-throw.</typeparam>
        public void Rethrow<TException>(Func<HttpContext, TException, bool> predicate) where TException : Exception
        {
            Rethrow((ctx, ex) => ex is TException exception && predicate(ctx, exception));
        }

        /// <summary>
        /// Configures the middleware to re-throw all exceptions. This can be useful if you
        /// have upstream middleware that needs to do additional handling of exceptions.
        /// </summary>
        public void RethrowAll()
        {
            RethrowPolicies.Clear(); // There's no point in keeping multiple policies
            Rethrow((_, _) => true); // when this one always returns true :)
        }

        /// <summary>
        /// Configures the middleware to re-throw exceptions, based on the specified <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to determine whether an exception should be re-thrown.</param>
        public void Rethrow(Func<HttpContext, Exception, bool> predicate)
        {
            RethrowPolicies.Add(predicate);
        }

        internal bool ShouldRethrowException(HttpContext httpContext, Exception exception)
        {
            foreach (var policy in RethrowPolicies)
            {
                if (policy(httpContext, exception))
                {
                    return true;
                }
            }

            return false;
        }

        internal void CallBeforeWriteHook(HttpContext context, MvcProblemDetails details)
        {
            AddTraceId(context, details);
            OnBeforeWriteDetails?.Invoke(context, details);
#if NETCOREAPP3_1
            // Workaround for https://github.com/dotnet/aspnetcore/pull/17565.
            context.Response.StatusCode = details.Status ?? context.Response.StatusCode;
#endif
        }

        private void AddTraceId(HttpContext context, MvcProblemDetails details)
        {
            var key = TraceIdPropertyName;

            if (details.Extensions.ContainsKey(key))
            {
                return;
            }

            var traceId = GetTraceId.Invoke(context);

            if (!string.IsNullOrEmpty(traceId))
            {
                details.Extensions[key] = traceId;
            }
        }

        internal bool TryMapProblemDetails(HttpContext context, Exception? exception, out MvcProblemDetails? problem)
        {
            if (exception is null)
            {
                problem = default;
                return false;
            }

            foreach (var mapper in Mappers)
            {
                if (mapper.TryMap(context, exception, out problem))
                {
                    return true;
                }
            }

            problem = default;
            return false;
        }

        private sealed class ExceptionMapper
        {
            public ExceptionMapper(Type type, Func<HttpContext, Exception, MvcProblemDetails?> mapping, Func<HttpContext, Exception, bool> predicate)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
                Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            }

            private Type Type { get; }

            private Func<HttpContext, Exception, MvcProblemDetails?> Mapping { get; }

            private Func<HttpContext, Exception, bool> Predicate { get; }

            public bool ShouldMap(HttpContext context, Exception exception)
            {
                return Type.IsInstanceOfType(exception) && Predicate(context, exception);
            }

            public bool TryMap(HttpContext context, Exception exception, out MvcProblemDetails? problem)
            {
                if (ShouldMap(context, exception))
                {
                    try
                    {
                        problem = Mapping(context, exception);
                        return true;
                    }
                    catch
                    {
                        problem = default;
                        return false;
                    }
                }

                problem = default;
                return false;
            }
        }
    }
}
