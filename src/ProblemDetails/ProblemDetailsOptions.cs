using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsOptions
    {
        public ProblemDetailsOptions()
        {
            SourceCodeLineCount = 6;
            Mappings = new Dictionary<Type, Func<Exception, MvcProblemDetails>>();
        }

        public int SourceCodeLineCount { get; set; }

        public IFileProvider FileProvider { get; set; }

        public Func<HttpContext, bool> IncludeExceptionDetails { get; set; }

        private Dictionary<Type, Func<Exception, MvcProblemDetails>> Mappings { get; }

        public void Map<TException>(Func<TException, MvcProblemDetails> mapping) where TException : Exception
        {
            Mappings[typeof(TException)] = ex => mapping((TException) ex);
        }

        internal bool TryMap<TException>(Func<TException, MvcProblemDetails> mapping) where TException : Exception
        {
            if (Mappings.ContainsKey(typeof(TException)))
            {
                return false;
            }

            Map(mapping);
            return true;
        }

        internal bool TryMapProblemDetails(Exception exception, out MvcProblemDetails problem)
        {
            var type = exception.GetType();

            if (Mappings.TryGetValue(type, out var mapping))
            {
                try
                {
                    problem = mapping(exception);
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
