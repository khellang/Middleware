namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using FluentValidation;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

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
        public Dictionary<string, string[]> ValidationErrors { get; set; }

        [JsonIgnore]
        [XmlElement("ValidationErrors")]
        [DataMember(Name = "ValidationErrors", Order = 600, EmitDefaultValue = false)]
        public ValidationErrorDetails ValidationErrorsProxy
        {
            get => new ValidationErrorDetails(ValidationErrors);
            set => ValidationErrors = value.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }

        [CollectionDataContract(ItemName = "ValidationError", KeyName = "Field", ValueName = "Errors", Namespace = "")]
        public class ValidationErrorDetails : Dictionary<string, Errors>
        {
            // WARNING: If you remove this ctor, the serializer is angry
            public ValidationErrorDetails() { }

            public ValidationErrorDetails(Dictionary<string, string[]> dictionary)
                : base(dictionary.ToDictionary(x => x.Key, x => new Errors(x.Value))) { }
        }

        [CollectionDataContract(ItemName = "Error", Namespace = "")]
        public class Errors : Collection<string>
        {
            // WARNING: If you remove this ctor, the serializer is angry
            public Errors() { }

            public Errors(IList<string> list) : base(list) { }
        }

        // Here to make DataContractSerializer happy
        public ValidationProblemDetails() : base(StatusCodes.Status400BadRequest)
        {
            Title = DefaultTitle;
            Detail = "Validatie mislukt!"; // TODO: Localize
            ProblemInstanceUri = GetProblemNumber();
            ProblemTypeUri = GetTypeUriFor<ValidationException>();
        }

        public ValidationProblemDetails(ValidationException exception) : this()
        {
            ValidationErrors = exception.Errors
                .GroupBy(x => x.PropertyName, y => y.ErrorMessage)
                .ToDictionary(x => x.Key, y => y.ToArray());
        }
    }
}
