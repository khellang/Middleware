namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using FluentValidation;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of Problem Details for HTTP APIs https://tools.ietf.org/html/rfc7807 with additional Validation Errors
    /// </summary>
    [DataContract(Name = "ValidationProblemDetails", Namespace = "")]
    public class ValidationProblemDetails : StatusCodeProblemDetails
    {
        /// <summary>Validatie fouten.</summary>
        [JsonProperty("validationErrors", Required = Required.DisallowNull)]
        [DataMember(Name = "validationErrors", Order = 600, EmitDefaultValue = false)]
        public string[] ValidationErrors { get; set; }

        // Here to make DataContractSerializer happy
        public ValidationProblemDetails() : base(StatusCodes.Status400BadRequest) { }

        public ValidationProblemDetails(ValidationException exception) : base(StatusCodes.Status400BadRequest)
        {
            Detail = "Validatie mislukt!"; // TODO: Localize
            ProblemInstanceUri = GetProblemNumber();
            ProblemTypeUri = GetTypeUriFor(exception);
            ValidationErrors = exception.Errors.Select(x => x.ErrorMessage).ToArray();
        }
    }
}
