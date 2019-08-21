namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System;
    using System.Text;

    public class ProblemDetailsException : Exception
    {
        public ProblemDetails Details { get; }

        public ProblemDetailsException(ProblemDetails details) : base($"{details.ProblemTypeUri} : {details.Title}")
            => Details = details;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Type    : {Details.ProblemTypeUri}");
            stringBuilder.AppendLine($"Title   : {Details.Title}");
            stringBuilder.AppendLine($"Status  : {Details.HttpStatus}");
            stringBuilder.AppendLine($"Detail  : {Details.Detail}");
            stringBuilder.AppendLine($"Instance: {Details.ProblemInstanceUri}");

            return stringBuilder.ToString();
        }
    }
}
