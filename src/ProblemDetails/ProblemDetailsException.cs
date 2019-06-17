using System;
using System.Text;

namespace Hellang.Middleware.ProblemDetails
{
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsException(Microsoft.AspNetCore.Mvc.ProblemDetails details)
            :base($"{details.Type} : {details.Title}")
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
