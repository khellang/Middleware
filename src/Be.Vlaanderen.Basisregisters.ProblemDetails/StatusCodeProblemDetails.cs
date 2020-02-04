namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System.Runtime.Serialization;
    using Microsoft.AspNetCore.WebUtilities;

    [DataContract(Name = "StatusCodeProblemDetails", Namespace = "")]
    public class StatusCodeProblemDetails : ProblemDetails
    {
        public StatusCodeProblemDetails(int statusCode)
        {
            HttpStatus = statusCode;
            ProblemTypeUri = $"https://httpstatuses.com/{statusCode}";
            Title = ReasonPhrases.GetReasonPhrase(statusCode);
        }
    }
}
