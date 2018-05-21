using System;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.ProblemDetails
{
    public class ExceptionProblemDetails : StatusCodeProblemDetails
    {
        public ExceptionProblemDetails(Exception error) : this(error, StatusCodes.Status500InternalServerError)
        {
        }

        public ExceptionProblemDetails(Exception error, int statusCode) : base(statusCode)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        public Exception Error { get; }
    }
}
