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

        public ProblemDetailsException(int statusCode, Exception innerException)
            : this(StatusCodeProblemDetails.Create(statusCode), innerException)
        {
        }

        public ProblemDetailsException(int statusCode, string title)
            : this(StatusCodeProblemDetails.Create(statusCode, title), null)
        {
        }

        public ProblemDetailsException(int statusCode, string title, Exception innerException)
            : this(StatusCodeProblemDetails.Create(statusCode, title), innerException)
        {
        }

        public ProblemDetailsException(MvcProblemDetails details)
            : this(details, null)
        {
            Details = details;
        }

        public ProblemDetailsException(MvcProblemDetails details, Exception? innerException)
            : base($"{details.Type} : {details.Title}", innerException)
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
