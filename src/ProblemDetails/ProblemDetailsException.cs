using System;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsException(Microsoft.AspNetCore.Mvc.ProblemDetails details)
        {
            Details = details;
        }

        public Microsoft.AspNetCore.Mvc.ProblemDetails Details { get; }
    }
}
