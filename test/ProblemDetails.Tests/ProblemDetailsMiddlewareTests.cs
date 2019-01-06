using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails.Tests.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;

namespace Hellang.Middleware.ProblemDetails.Tests
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
            using (var server = CreateServer(handler: ResponseWithStatusCode(expected)))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal(expected, response.StatusCode);
                await AssertIsProblemDetailsResponse(response);
            }
        }

        [Fact]
        public async Task Exception_IsHandled()
        {
            using (var server = CreateServer(handler: ResponseThrows()))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                await AssertIsProblemDetailsResponse(response);
            }
        }

        [Fact]
        public async Task ProblemDetailsException_IsHandled()
        {
            var problemStatus = HttpStatusCode.TooManyRequests;
            var ex = new ProblemDetailsException(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Too Many Requests",
                Status = (int) problemStatus,
            });
            using (var server = CreateServer(handler: ResponseThrows(ex)))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/");

                Assert.Equal(problemStatus, response.StatusCode);
                await AssertIsProblemDetailsResponse(response);
            }
        }

        [Fact]
        public async Task Catchall_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            using (var server = CreateServer(handler: ResponseThrows()))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                AssertUnhandledExceptionLogged(Logger);
            }
        }

        [Fact]
        public async Task Mapped_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            void MapNotImplementException(ProblemDetailsOptions options)
            {
                options.Map<NotImplementedException>(ex =>
                    new ExceptionProblemDetails(ex) {Status = StatusCodes.Status501NotImplemented});
            }

            using (var server = CreateServer(
                handler: ResponseThrows(new NotImplementedException()), 
                configureOptions: MapNotImplementException)
            )
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/");

                Assert.Equal((HttpStatusCode)StatusCodes.Status501NotImplemented, response.StatusCode);
                AssertUnhandledExceptionLogged(Logger);
            }
        }

        [Fact]
        public async Task Explicit_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            var ex = new ProblemDetailsException(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
            });
            using (var server = CreateServer(handler: ResponseThrows(ex)))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal((HttpStatusCode)StatusCodes.Status429TooManyRequests, response.StatusCode);
                AssertUnhandledExceptionNotLogged(Logger);
            }
        }

        [Fact]
        public async Task Mapped_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            void MapNotImplementException(ProblemDetailsOptions options)
            {
                options.Map<NotImplementedException>(ex =>
                    new ExceptionProblemDetails(ex) { Status = StatusCodes.Status403Forbidden });
            }

            using (var server = CreateServer(
                handler: ResponseThrows(new NotImplementedException()), 
                configureOptions: MapNotImplementException)
            )
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal((HttpStatusCode)StatusCodes.Status403Forbidden, response.StatusCode);
                AssertUnhandledExceptionNotLogged(Logger);
            }
        }

        [Theory]
        [InlineData("Staging", 84)]
        [InlineData("Production", 84)]
        [InlineData("Development", 2271)]
        public async Task ExceptionDetails_AreOnlyIncludedInDevelopment(string environment, int expectedLength)
        {
            using (var server = CreateServer(handler: ResponseThrows(), environment: environment))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(expectedLength, content.Length);
            }
        }

        [Fact]
        public async Task StatusCode_IsMaintainedWhenStrippingExceptionDetails()
        {
            using (var server = CreateServer(handler: ResponseThrows(), environment: "Production"))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        [Fact]
        public async Task CORSHeaders_AreMaintained()
        {
            using (var server = CreateServer(handler: ResponseThrows()))
            using (var client = server.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/");

                request.Headers.Add(HeaderNames.Origin, "localhost");

                var response = await client.SendAsync(request);

                Assert.Contains(response.Headers, x => x.Key.StartsWith("Access-Control-Allow-"));
            }
        }

        [Fact]
        public async Task ProblemResponses_ShouldNotBeCached()
        {
            using (var server = CreateServer(handler: ResponseThrows()))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                var cacheControl = response.Headers.CacheControl;

                Assert.True(cacheControl.NoCache, nameof(cacheControl.NoCache));
                Assert.True(cacheControl.NoStore, nameof(cacheControl.NoStore));
                Assert.True(cacheControl.MustRevalidate, nameof(cacheControl.MustRevalidate));
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData((HttpStatusCode) 600)]
        [InlineData((HttpStatusCode) 800)]
        public async Task SuccessStatusCode_IsNotHandled(HttpStatusCode expected)
        {
            using (var server = CreateServer(handler: ResponseWithStatusCode(expected)))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal(expected, response.StatusCode);
                Assert.Equal(0, response.Content.Headers.ContentLength);
            }
        }

        [Fact]
        public async Task StartedResponse_IsNotHandled()
        {
            using (var server = CreateServer(handler: context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.Body.WriteByte(byte.MinValue);
                return Task.CompletedTask;
            }))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");

                Assert.Equal(1, response.Content.Headers.ContentLength);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        [Fact]
        public async Task Exceptions_After_Reponse_Started_IsNotHandled()
        {
            using (var server = CreateServer(handler: context =>
            {
                context.Response.Body.WriteByte(byte.MinValue);
                throw new Exception("Request Failed");
            }))
            using (var client = server.CreateClient())
            {
                await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    var response = await client.GetAsync("");

                    Assert.Equal(1, response.Content.Headers.ContentLength);
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                });
            }
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException()
        {
            var ex = new ProblemDetailsException(new EvilProblemDetails());
            using (var server = CreateServer(handler: ResponseThrows(ex)))
            using (var client = server.CreateClient())
            {
                await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    var response = await client.GetAsync("");

                    Assert.Equal(1, response.Content.Headers.ContentLength);
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                });
            }
        }

        [Fact]
        public async Task Options_OnBeforeWriteDetails()
        {
            using (var server = CreateServer(handler: ResponseThrows()))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Contains("\"type\":\"https://httpstatuses.com/500\"", content);
            }

            void ConfigureOptions(ProblemDetailsOptions options)
            {
                options.OnBeforeWriteDetails = details =>
                {
                    details.Type = "https://example.com";
                };
            }

            using (var server = CreateServer(handler: ResponseThrows(), configureOptions: ConfigureOptions))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("");
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Contains("\"type\":\"https://example.com\"", content);
            }
        }

        private static async Task AssertIsProblemDetailsResponse(HttpResponseMessage response)
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
            Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        }

        private static void AssertUnhandledExceptionLogged(InMemoryLogger<ProblemDetailsMiddleware> logger)
        {
            Assert.Single(logger.Messages.Where(m => m.Type == LogLevel.Error));
        }

        private static void AssertUnhandledExceptionNotLogged(InMemoryLogger<ProblemDetailsMiddleware> logger)
        {
            Assert.Empty(logger.Messages.Where(m => m.Type == LogLevel.Error));
        }

        private TestServer CreateServer(RequestDelegate handler,
            Action<ProblemDetailsOptions> configureOptions = null, string environment = null)
        {
            var builder = new WebHostBuilder()
                .UseEnvironment(environment ?? EnvironmentName.Development)
                .ConfigureServices(x => x
                    .AddSingleton<ILogger<ProblemDetailsMiddleware>>(Logger)
                    .AddCors()
                    .AddProblemDetails(configureOptions)
                    .AddMvcCore()
                    .AddJsonFormatters(ConfigureJson))
                .Configure(x => x
                    .UseCors(y => y.AllowAnyOrigin())
                    .UseProblemDetails()
                    .Run(handler));

            return new TestServer(builder);
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
            return context => throw exception ?? new Exception();
        }

        private static void ConfigureJson(JsonSerializerSettings json)
        {
            json.NullValueHandling = NullValueHandling.Ignore;
        }

        private class EvilProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            public string EvilProperty => throw new Exception("This should throw during serialization.");
        }
    }
}
