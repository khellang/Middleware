namespace Be.Vlaanderen.Basisregisters.BasicApiProblem.Tests
{
    using Xunit;

    public class ProblemDetailsExceptionTests
    {
        private static ProblemDetails CreateProblemDetails()
            => new ProblemDetails
            {
                ProblemTypeUri = "https://httpstatuses.com/303",
                Title = "See other",
                HttpStatus = 303,
                Detail = "Look somewhere else.",
                ProblemInstanceUri = "https://example.com/problem/123",
            };

        [Fact]
        public void Constructor_InitializesMessage()
        {
            var problemDetails = CreateProblemDetails();

            var exception = new ProblemDetailsException(problemDetails);

            Assert.Equal("https://httpstatuses.com/303 : See other", exception.Message);
        }

        [Fact]
        public void ToString_ReturnsAllDetails()
        {
            var problemDetails = CreateProblemDetails();

            var exception = new ProblemDetailsException(problemDetails);
            var actual = exception.ToString();

            var expected = @"Type    : https://httpstatuses.com/303
Title   : See other
Status  : 303
Detail  : Look somewhere else.
Instance: https://example.com/problem/123
";

            Assert.Equal(expected, actual);
        }
    }
}
