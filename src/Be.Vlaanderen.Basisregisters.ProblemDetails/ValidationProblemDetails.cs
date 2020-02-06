namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System.Collections.Generic;
    using System.ComponentModel;
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
        [Description("Validatie fouten.")]
        public Dictionary<string, string[]> ValidationErrors { get; set; }

        // Here to make DataContractSerializer happy
        public ValidationProblemDetails() : base(StatusCodes.Status400BadRequest)
        {
            Detail = "Validatie mislukt!"; // TODO: Localize
            ProblemInstanceUri = GetProblemNumber();
            ProblemTypeUri = GetTypeUriFor(new ValidationException("irrelevant"));
        }

        public ValidationProblemDetails(ValidationException exception) : this()
        {
            ValidationErrors = exception.Errors
                .GroupBy(x => x.PropertyName, y => y.ErrorMessage)
                .ToDictionary(x => x.Key, y => y.ToArray());
        }
    }
}
