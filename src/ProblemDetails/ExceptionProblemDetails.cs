using System;
using System.Text.Json.Serialization;
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

        [JsonIgnore]
        public Exception Error { get; set; }
    }
}
