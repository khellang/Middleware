using Microsoft.AspNetCore.WebUtilities;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    /// <summary>
    /// A basic problem details representation for an HTTP status code.
    /// It includes default values for <see cref="MvcProblemDetails.Type"/> and <see cref="MvcProblemDetails.Title"/>.
    /// </summary>
    public class StatusCodeProblemDetails : MvcProblemDetails
    {
        public StatusCodeProblemDetails(int statusCode)
        {
            Status = statusCode;
            Type = $"https://httpstatuses.com/{statusCode}";
            Title = ReasonPhrases.GetReasonPhrase(statusCode);
        }
    }
}
