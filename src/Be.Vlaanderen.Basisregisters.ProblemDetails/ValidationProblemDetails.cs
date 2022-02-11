namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using FluentValidation;
    using FluentValidation.Results;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;

    /// <summary>
    /// Implementation of Problem Details for HTTP APIs https://tools.ietf.org/html/rfc7807 with additional Validation Errors
    /// </summary>
    [DataContract(Name = "ProblemDetails", Namespace = "")]
    public class ValidationProblemDetails : StatusCodeProblemDetails
    {
        /// <summary>Validatie fouten.</summary>
        [XmlIgnore]
        [IgnoreDataMember]
        [JsonProperty("validationErrors", Required = Required.DisallowNull)]
        [Description("Validatie fouten.")]
        public Dictionary<string, Errors> ValidationErrors { get; set; }

        [JsonIgnore]
        [XmlElement("ValidationErrors")]
        [DataMember(Name = "ValidationErrors", Order = 600, EmitDefaultValue = false)]
        public ValidationErrorDetails ValidationErrorsProxy
        {
            get => new ValidationErrorDetails(ValidationErrors);
            set => ValidationErrors = value;
        }

        [CollectionDataContract(ItemName = "ValidationError", KeyName = "Field", ValueName = "Errors", Namespace = "")]
        public class ValidationErrorDetails : Dictionary<string, Errors>
        {
            // WARNING: If you remove this ctor, the serializer is angry
            public ValidationErrorDetails() { }

            public ValidationErrorDetails(Dictionary<string, Errors> dictionary)
                : base(dictionary) { }
        }

        [CollectionDataContract(ItemName = "Error", Namespace = "")]
        public class Errors : Collection<ValidationError>
        {
            // WARNING: If you remove this ctor, the serializer is angry
            public Errors() { }

            public Errors(IList<ValidationError> list) : base(list) { }
        }

        // Here to make DataContractSerializer happy
        public ValidationProblemDetails()
            : base(StatusCodes.Status400BadRequest)
        {
            Title = DefaultTitle;
            Detail = "Validatie mislukt!"; // TODO: Localize
            ProblemInstanceUri = GetProblemNumber();
            ProblemTypeUri = GetTypeUriFor<ValidationException>();
        }

        public ValidationProblemDetails(ValidationException exception) : this()
        {
            ValidationErrors = exception.Errors
                .GroupBy(x => x.PropertyName, x => x)
                .ToDictionary(x => x.Key, x => new Errors(x.Select(y => new ValidationError(y)).ToList()));
        }
    }

    public class ValidationError
    {
        [DataMember(Name = "Code", EmitDefaultValue = false)]
        public string? Code { get; set; }

        [DataMember(Name = "Reason")]
        [JsonProperty("reason", Required = Required.DisallowNull)]
        public string Reason { get; set; } = "";

        public ValidationError()
        { }

        public ValidationError(string reason)
        {
            Reason = reason;
        }

        public ValidationError(string code, string reason)
        {
            Code = code;
            Reason = reason;
        }

        public ValidationError(ValidationFailure failure)
        {
            Code = failure.ErrorCode;
            Reason = failure.ErrorMessage;
        }
    }
}
