using Microsoft.AspNetCore.WebUtilities;

namespace Hellang.Middleware.ProblemDetails
{
    public class StatusCodeProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public StatusCodeProblemDetails(int statusCode)
        {
            Status = statusCode;
            Type = $"https://httpstatuses.com/{statusCode}";
            Title = ReasonPhrases.GetReasonPhrase(statusCode);
        }
    }
}
