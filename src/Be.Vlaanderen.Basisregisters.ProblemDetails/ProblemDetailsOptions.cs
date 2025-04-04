namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.Collections.Generic;

    public class ProblemDetailsOptions
    {
        public Func<HttpContext, bool>? IsProblem { get; set; }

        public Func<HttpContext, int, ProblemDetails>? MapStatusCode { get; set; }

        public Action<HttpContext, ProblemDetails>? OnBeforeWriteDetails { get; set; }

        public Func<HttpContext, Exception, ProblemDetails, bool>? ShouldLogUnhandledException { get; set; }

        public HashSet<string> AllowedHeaderNames { get; }

        private List<ExceptionMapper> Mappers { get; }

        public ProblemDetailsOptions()
        {
            Mappers = new List<ExceptionMapper>();
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

        public void Map<TException>(Func<TException, ProblemDetails> mapping) where TException : Exception
            => Map<TException>((_, ex) => mapping(ex));

        public void Map<TException>(Func<HttpContext, TException, ProblemDetails> mapping) where TException : Exception
            => Mappers.Add(new ExceptionMapper(typeof(TException), (ctx, ex) => mapping(ctx, (TException)ex)));

        internal bool TryMapProblemDetails(HttpContext context, Exception exception, out ProblemDetails problem)
        {
            foreach (var mapper in Mappers)
                if (mapper.TryMap(context, exception, out problem))
                    return true;

            problem = default;
            return false;
        }

        private sealed class ExceptionMapper
        {
            private Type Type { get; }

            private Func<HttpContext, Exception, ProblemDetails> Mapping { get; }

            public ExceptionMapper(
                Type type,
                Func<HttpContext, Exception, ProblemDetails> mapping)
            {
                Type = type;
                Mapping = mapping;
            }

            public bool CanMap(Type type) => Type.IsAssignableFrom(type);

            public bool TryMap(HttpContext context, Exception exception, out ProblemDetails problem)
            {
                if (CanMap(exception.GetType()))
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
