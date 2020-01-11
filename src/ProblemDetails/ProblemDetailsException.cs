using System;
using System.Net;
using System.Text;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsException(HttpStatusCode statusCode) : this((int)statusCode)
        {
        }

        public ProblemDetailsException(int statusCode) : this(new StatusCodeProblemDetails(statusCode))
        {
        }

        public ProblemDetailsException(HttpStatusCode statusCode, string title) : this((int)statusCode, title)
        {
        }

        public ProblemDetailsException(int statusCode, string title) : this(new StatusCodeProblemDetails(statusCode) { Title = title })
        {
        }

        public ProblemDetailsException(Microsoft.AspNetCore.Mvc.ProblemDetails details)
             : base($"{details.Type} : {details.Title}")
        {
            Details = details;
        }

        public Microsoft.AspNetCore.Mvc.ProblemDetails Details { get; }

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
