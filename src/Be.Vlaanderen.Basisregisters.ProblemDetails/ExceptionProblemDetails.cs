namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using System;
    using System.Runtime.Serialization;

    [DataContract(Name = "ExceptionProblemDetails", Namespace = "")]
    public class ExceptionProblemDetails : StatusCodeProblemDetails
    {
        [JsonProperty("error", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        [DataMember(Name = "error", Order = 600, EmitDefaultValue = false)]
        public Exception Error { get; }

        // Here to make DataContractSerializer happy
        public ExceptionProblemDetails() : base(StatusCodes.Status500InternalServerError) { }

        public ExceptionProblemDetails(Exception error) : this(error, StatusCodes.Status500InternalServerError) { }

        public ExceptionProblemDetails(Exception error, int statusCode) : base(statusCode)
            => Error = error ?? throw new ArgumentNullException(nameof(error));
    }
}
