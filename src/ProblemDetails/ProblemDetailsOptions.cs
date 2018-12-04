using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsOptions
    {
        public ProblemDetailsOptions()
        {
            SourceCodeLineCount = 6;
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

        public int SourceCodeLineCount { get; set; }

        public IFileProvider FileProvider { get; set; }

        public Func<HttpContext, bool> IncludeExceptionDetails { get; set; }

        public Func<HttpContext, bool> IsProblem { get; set; }

        public Func<int, MvcProblemDetails> MapStatusCode { get; set; }
        public Action<MvcProblemDetails> OnBeforeWriteDetails { get; set; }

        public HashSet<string> AllowedHeaderNames { get; }

        private List<ExceptionMapper> Mappers { get; }

        public void Map<TException>(Func<TException, MvcProblemDetails> mapping) where TException : Exception
        {
            Mappers.Add(new ExceptionMapper(typeof(TException), ex => mapping((TException) ex)));
        }

        internal bool TryMapProblemDetails(Exception exception, out MvcProblemDetails problem)
        {
            foreach (var mapper in Mappers)
            {
                if (mapper.TryMap(exception, out problem))
                {
                    return true;
                }
            }

            problem = default;
            return false;
        }

        private sealed class ExceptionMapper
        {
            public ExceptionMapper(Type type, Func<Exception, MvcProblemDetails> mapping)
            {
                Type = type;
                Mapping = mapping;
            }

            private Type Type { get; }

            private Func<Exception, MvcProblemDetails> Mapping { get; }

            public bool CanMap(Type type)
            {
                return Type.IsAssignableFrom(type);
            }

            public bool TryMap(Exception exception, out MvcProblemDetails problem)
            {
                if (CanMap(exception.GetType()))
                {
                    try
                    {
                        problem = Mapping(exception);
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
