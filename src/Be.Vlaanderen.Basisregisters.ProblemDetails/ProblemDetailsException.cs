namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using System;

    public class ProblemDetailsException : Exception
    {
        public ProblemDetails Details { get; }

        public ProblemDetailsException(ProblemDetails details) => Details = details;
    }
}
