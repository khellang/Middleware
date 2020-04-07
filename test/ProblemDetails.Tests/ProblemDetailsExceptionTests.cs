using Hellang.Middleware.ProblemDetails;
using Xunit;
using ReasonPhrases = Microsoft.AspNetCore.WebUtilities.ReasonPhrases;

namespace ProblemDetails.Tests
{
    public class ProblemDetailsExceptionTests
    {
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

            Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Constructor_FromHttpStatusCode()
        {
            var exception = new ProblemDetailsException(400);

            Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(exception.Details);

            Assert.Equal(400, exception.Details.Status);
            Assert.Equal(ReasonPhrases.GetReasonPhrase(400), exception.Details.Title);
        }

        [Fact]
        public void Constructor_FromHttpStatusCodeAndTitle()
        {
            var exception = new ProblemDetailsException(400, "foobar");

            Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(exception.Details);

            Assert.Equal(400, exception.Details.Status);
            Assert.Equal("foobar", exception.Details.Title);
        }

        private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails()
        {
            return new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = "https://httpstatuses.com/303",
                Title = "See other",
                Status = 303,
                Detail = "Look somewhere else.",
                Instance = "https://example.com/problem/123",
            };
        }
    }
}
