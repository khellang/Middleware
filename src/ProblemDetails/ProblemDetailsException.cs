using System;
using System.Text;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    /// <summary>
    /// An exception for passing an <see cref="MvcProblemDetails"/> instance to
    /// be handled by the <see cref="ProblemDetailsMiddleware"/>.
    /// </summary>
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsException(int statusCode)
            : this(StatusCodeProblemDetails.Create(statusCode))
        {
        }

        public ProblemDetailsException(int statusCode, string title)
            : this(StatusCodeProblemDetails.Create(statusCode, title))
        {
        }

        public ProblemDetailsException(MvcProblemDetails details)
            : base($"{details.Type} : {details.Title}")
        {
            Details = details;
        }

        public MvcProblemDetails Details { get; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Type    : {Details.Type}");
            stringBuilder.AppendLine($"Title   : {Details.Title}");
            stringBuilder.AppendLine($"Status  : {Details.Status}");
            stringBuilder.AppendLine($"Detail  : {Details.Detail}");
            stringBuilder.AppendLine($"Instance: {Details.Instance}");

            return stringBuilder.ToString();
        }
    }
}
