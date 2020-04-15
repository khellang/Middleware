using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.ProblemDetails
{
    [ExcludeFromCodeCoverage]
    [Obsolete("From v5.0, the ProblemDetails middleware will automatically include exception details for all exceptions. " +
              "Because of that, there's no point in using this class. StatusCodeProblemDetails should be used instead.")]
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
