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
            SetDetails(this, statusCode);
        }

        public static MvcProblemDetails Create(int statusCode)
        {
            var details = new MvcProblemDetails();

            SetDetails(details, statusCode);

            return details;
        }

        internal static MvcProblemDetails Create(int statusCode, string title)
        {
            var details = Create(statusCode);

            details.Title = title;

            return details;
        }

        private static void SetDetails(MvcProblemDetails details, int statusCode)
        {
            details.Status = statusCode;
            details.Type = GetDefaultType(statusCode);
            details.Title = ReasonPhrases.GetReasonPhrase(statusCode);
        }

        internal static string GetDefaultType(int statusCode)
        {
            return $"https://httpstatuses.com/{statusCode}";
        }
    }
}
