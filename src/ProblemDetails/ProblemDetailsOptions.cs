using System;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsOptions
    {
        public Func<HttpContext, bool> IncludeExceptionDetails { get; set; }
    }
}
