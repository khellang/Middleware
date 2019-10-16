using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProblemDetails.Tests.Helpers;
using Xunit;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

#if NETCOREAPP2_2
using Environments = Microsoft.Extensions.Hosting.EnvironmentName;
#else
using Environments = Microsoft.Extensions.Hosting.Environments;
#endif

namespace ProblemDetails.Tests
{
    public class ProblemDetailsMiddlewareTests
    {
        public ProblemDetailsMiddlewareTests()
        {
            Logger = new InMemoryLogger<ProblemDetailsMiddleware>();
        }

        private InMemoryLogger<ProblemDetailsMiddleware> Logger { get; }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotImplemented)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task ErrorStatusCode_IsHandled(HttpStatusCode expected)
        {
            using var client = CreateClient(handler: ResponseWithStatusCode(expected));

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(expected, response.StatusCode);
            await AssertIsProblemDetailsResponse(response);
        }

        [Fact]
        public async Task Exception_IsHandled()
        {
            using var client = CreateClient(handler: ResponseThrows());

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await AssertIsProblemDetailsResponse(response);
        }

        [Fact]
        public async Task ProblemDetailsException_IsHandled()
        {
            var expected = HttpStatusCode.TooManyRequests;

            var details = new MvcProblemDetails
            {
                Title = "Too Many Requests",
                Status = (int) expected,
            };

            var ex = new ProblemDetailsException(details);

            using var client = CreateClient(handler: ResponseThrows(ex));

            var response = await client.GetAsync("/");

            Assert.Equal(expected, response.StatusCode);
            await AssertIsProblemDetailsResponse(response);
        }

        [Fact]
        public async Task Catchall_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            using var client = CreateClient(handler: ResponseThrows());

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            AssertUnhandledExceptionLogged(Logger);
        }

        [Fact]
        public async Task Mapped_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            void MapNotImplementException(ProblemDetailsOptions options)
            {
                options.Map<NotImplementedException>(ex =>
                    new ExceptionProblemDetails(ex) {Status = StatusCodes.Status501NotImplemented});
            }

            var handler = ResponseThrows(new NotImplementedException());

            using var client = CreateClient(handler, MapNotImplementException);

            var response = await client.GetAsync("/");

            Assert.Equal((HttpStatusCode)StatusCodes.Status501NotImplemented, response.StatusCode);
            AssertUnhandledExceptionLogged(Logger);
        }

        [Fact]
        public async Task Explicit_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            var details = new MvcProblemDetails
            {
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
            };

            var ex = new ProblemDetailsException(details);

            using var client = CreateClient(handler: ResponseThrows(ex));

            var response = await client.GetAsync(string.Empty);

            Assert.Equal((HttpStatusCode)StatusCodes.Status429TooManyRequests, response.StatusCode);
            AssertUnhandledExceptionNotLogged(Logger);
        }

        [Fact]
        public async Task Mapped_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            void MapNotImplementException(ProblemDetailsOptions options)
            {
                options.Map<NotImplementedException>(ex =>
                    new ExceptionProblemDetails(ex) { Status = StatusCodes.Status403Forbidden });
            }

            var handler = ResponseThrows(new NotImplementedException());

            using var client = CreateClient(handler, MapNotImplementException);

            var response = await client.GetAsync(string.Empty);

            Assert.Equal((HttpStatusCode)StatusCodes.Status403Forbidden, response.StatusCode);
            AssertUnhandledExceptionNotLogged(Logger);
        }

        [Theory]
        [InlineData("Staging", 84)]
        [InlineData("Production", 84)]
        [InlineData("Development", 1400)]
        public async Task ExceptionDetails_AreOnlyIncludedInDevelopment(string environment, int expectedMinimumLength)
        {
            using var client = CreateClient(handler: ResponseThrows(), environment: environment);

            var response = await client.GetAsync(string.Empty);

            var content = await response.Content.ReadAsStringAsync();

            Assert.InRange(content.Length, expectedMinimumLength, int.MaxValue);
        }

        [Fact]
        public async Task StatusCode_IsMaintainedWhenStrippingExceptionDetails()
        {
            using var client = CreateClient(handler: ResponseThrows(), environment: "Production");

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task CORSHeaders_AreMaintained()
        {
            using var client = CreateClient(handler: ResponseThrows());

            var request = new HttpRequestMessage(HttpMethod.Get, "/");

            request.Headers.Add(HeaderNames.Origin, "localhost");

            var response = await client.SendAsync(request);

            Assert.Contains(response.Headers, x => x.Key.StartsWith("Access-Control-Allow-"));
        }

        [Fact]
        public async Task ProblemResponses_ShouldNotBeCached()
        {
            using var client = CreateClient(handler: ResponseThrows());

            var response = await client.GetAsync(string.Empty);

            var cacheControl = response.Headers.CacheControl;

            Assert.True(cacheControl.NoCache, nameof(cacheControl.NoCache));
            Assert.True(cacheControl.NoStore, nameof(cacheControl.NoStore));
            Assert.True(cacheControl.MustRevalidate, nameof(cacheControl.MustRevalidate));
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData((HttpStatusCode) 600)]
        [InlineData((HttpStatusCode) 800)]
        public async Task SuccessStatusCode_IsNotHandled(HttpStatusCode expected)
        {
            using var client = CreateClient(handler: ResponseWithStatusCode(expected));

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(expected, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
        }

        [Fact]
        public async Task StartedResponse_IsNotHandled()
        {
            Task WriteResponse(HttpContext context)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return context.Response.WriteAsync("hello");
            }

            using var client = CreateClient(handler: WriteResponse);

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(5, response.Content.Headers.ContentLength);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Exceptions_After_Response_Started_IsNotHandled()
        {
            async Task WriteResponse(HttpContext context)
            {
                await context.Response.WriteAsync("hello");
                throw new Exception("Request Failed");
            }

            using var client = CreateClient(handler: WriteResponse);

            await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException()
        {
            var ex = new ProblemDetailsException(new EvilProblemDetails());

            using var client = CreateClient(handler: ResponseThrows(ex));

            await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task Options_OnBeforeWriteDetails()
        {
            var wasCalled = false;

            void ConfigureOptions(ProblemDetailsOptions options)
            {
                options.OnBeforeWriteDetails = (ctx, details) => wasCalled = true;
            }

            using var client = CreateClient(handler: ResponseThrows(), ConfigureOptions);

            var response = await client.GetAsync(string.Empty);

            Assert.True(wasCalled);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private static async Task AssertIsProblemDetailsResponse(HttpResponseMessage response)
        {
            Assert.NotEmpty(await response.Content.ReadAsStringAsync());
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
        }

        private static void AssertUnhandledExceptionLogged(InMemoryLogger<ProblemDetailsMiddleware> logger)
        {
            Assert.Single(logger.Messages.Where(m => m.Type == LogLevel.Error));
        }

        private static void AssertUnhandledExceptionNotLogged(InMemoryLogger<ProblemDetailsMiddleware> logger)
        {
            Assert.Empty(logger.Messages.Where(m => m.Type == LogLevel.Error));
        }

        private HttpClient CreateClient(RequestDelegate handler, Action<ProblemDetailsOptions> configureOptions = null, string environment = null)
        {
            var builder = new WebHostBuilder()
                .UseEnvironment(environment ?? Environments.Development)
                .ConfigureServices(x => x
                    .AddSingleton<ILogger<ProblemDetailsMiddleware>>(Logger)
                    .AddCors()
                    .AddProblemDetails(configureOptions)
                    .AddMvcCore()
                    .AddJson())
                .Configure(x => x
                    .UseCors(y => y.AllowAnyOrigin())
                    .UseProblemDetails()
                    .Run(handler));

            var server = new TestServer(builder);

            var client = server.CreateClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/problem+json");

            return client;
        }

        private static RequestDelegate ResponseWithStatusCode(HttpStatusCode statusCode)
        {
            return context =>
            {
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            };
        }

        private static RequestDelegate ResponseThrows(Exception exception = null)
        {
            return context => throw exception ?? new Exception("Request failed.");
        }

        private class EvilProblemDetails : MvcProblemDetails
        {
            public string EvilProperty => throw new Exception("This should throw during serialization.");
        }
    }
}
