using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotImplemented)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task ErrorStatusCode_IsHandled(HttpStatusCode expected)
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync($"/error/{(int)expected}");

                Assert.Equal(expected, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
                Assert.NotEmpty(await response.Content.ReadAsStringAsync());
            }
        }

        [Theory]
        [InlineData("/exception", HttpStatusCode.InternalServerError)]
        [InlineData("/exception-details", (HttpStatusCode) StatusCodes.Status429TooManyRequests)]
        public async Task Exception_IsHandled(string path, HttpStatusCode expected)
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync(path);

                Assert.Equal(expected, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
                Assert.NotEmpty(await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Catchall_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                AssertUnhandledExceptionLogged();
            }
        }

        [Fact]
        public async Task Mapped_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            void MapNotImplementException(ProblemDetailsOptions options)
            {
                options.Map<NotImplementedException>(
                    ex => new ExceptionProblemDetails(ex) {Status = StatusCodes.Status501NotImplemented});
            }

            using (var server = CreateServer(configureOptions: MapNotImplementException))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception");

                Assert.Equal((HttpStatusCode)StatusCodes.Status501NotImplemented, response.StatusCode);
                AssertUnhandledExceptionLogged();
            }
        }

        [Fact]
        public async Task Explicit_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception-details");

                Assert.Equal((HttpStatusCode)StatusCodes.Status429TooManyRequests, response.StatusCode);
                AssertUnhandledExceptionNotLogged();
            }
        }

        [Fact]
        public async Task Mapped_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            void MapNotImplementException(ProblemDetailsOptions options)
            {
                options.Map<NotImplementedException>(
                    ex => new ExceptionProblemDetails(ex) { Status = StatusCodes.Status403Forbidden });
            }

            using (var server = CreateServer(configureOptions: MapNotImplementException))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception");

                Assert.Equal((HttpStatusCode)StatusCodes.Status403Forbidden, response.StatusCode);
                AssertUnhandledExceptionNotLogged();
            }
        }

        [Theory]
        [InlineData("Staging", 84)]
        [InlineData("Production", 84)]
        [InlineData("Development", 2550)]
        public async Task ExceptionDetails_AreOnlyIncludedInDevelopment(string environment, int expectedLength)
        {
            using (var server = CreateServer(environment))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception");

                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(expectedLength, content.Length);
            }
        }

        [Fact]
        public async Task StatusCode_IsMaintainedWhenStrippingExceptionDetails()
        {
            using (var server = CreateServer("Production"))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        [Fact]
        public async Task CORSHeaders_AreMaintained()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/exception");

                request.Headers.Add(HeaderNames.Origin, "localhost");

                var response = await client.SendAsync(request);

                Assert.Contains(response.Headers, x => x.Key.StartsWith("Access-Control-Allow-"));
            }
        }

        [Fact]
        public async Task ProblemResponses_ShouldNotBeCached()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/exception");

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
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync($"/success/{(int)expected}");

                Assert.Equal(expected, response.StatusCode);
                Assert.Equal(0, response.Content.Headers.ContentLength);

            }
        }

        [Fact]
        public async Task StartedResponse_IsNotHandled()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/error-started");

                Assert.Equal(1, response.Content.Headers.ContentLength);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    response = await client.GetAsync("/exception-started");

                    Assert.Equal(1, response.Content.Headers.ContentLength);
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                });
            }
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    var response = await client.GetAsync("/exception-bad");

                    Assert.Equal(1, response.Content.Headers.ContentLength);
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                });
            }
        }

        [Fact]
        public async Task Options_OnBeforeWriteDetails()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/error/500");
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Contains("\"type\":\"https://httpstatuses.com/500\"", content);
            }

            void ConfigureOptions(ProblemDetailsOptions options)
            {
                options.OnBeforeWriteDetails = details => {
                    details.Type = "https://example.com";
                };
            }

            using (var server = CreateServer(configureOptions: ConfigureOptions))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/error/500");
                var content = await response.Content.ReadAsStringAsync();
                
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Contains("\"type\":\"https://example.com\"", content);
            }
        }

        private void AssertUnhandledExceptionLogged()
        {
            var logMessage = Logger.Messages.FirstOrDefault(m => m.Type == LogLevel.Error);
            Assert.NotNull(logMessage);
        }

        private void AssertUnhandledExceptionNotLogged()
        {
            var logMessage = Logger.Messages.FirstOrDefault(m => m.Type == LogLevel.Error);
            Assert.Null(logMessage);
        }

        private TestServer CreateServer(string environment = null, Action<ProblemDetailsOptions> configureOptions = null)
        {
            Logger = new InMemoryLogger<ProblemDetailsMiddleware>();
            var builder = new WebHostBuilder()
                .UseEnvironment(environment ?? EnvironmentName.Development)
                .ConfigureServices(x => x
                    .AddTransient<ILogger<ProblemDetailsMiddleware>>(_ => Logger)
                    .AddCors()
                    .AddProblemDetails(configureOptions)
                    .AddMvcCore()
                    .AddJsonFormatters(ConfigureJson))
                .Configure(x => x
                    .UseCors(y => y.AllowAnyOrigin())
                    .UseProblemDetails()
                    .Use(HandleRoutes));

            return new TestServer(builder);
        }

        private InMemoryLogger<ProblemDetailsMiddleware> Logger { get; set; }

        private static Task HandleRoutes(HttpContext context, Func<Task> next)
        {
            if (TryGetStatusCode(context, "/success", out var statusCode))
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }

            if (context.Request.Path.StartsWithSegments("/error-started"))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.Body.WriteByte(byte.MinValue);
                return Task.CompletedTask;
            }
            
            if (TryGetStatusCode(context, "/error", out statusCode))
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }

            if (context.Request.Path.StartsWithSegments("/exception-details"))
            {
                throw new ProblemDetailsException(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                });
            }

            if (context.Request.Path.StartsWithSegments("/exception-bad"))
            {
                throw new ProblemDetailsException(new EvilProblemDetails());
            }

            if (context.Request.Path.StartsWithSegments("/exception-started"))
            {
                context.Response.Body.WriteByte(byte.MinValue);
                throw new Exception("Request Failed");
            }
            
            if (context.Request.Path.StartsWithSegments("/exception"))
            {
                throw new NotImplementedException();
            }

            return next();
        }

        private static bool TryGetStatusCode(HttpContext context, string path, out int statusCode)
        {
            if (context.Request.Path.StartsWithSegments(path, out _, out var remaining))
            {
                if (int.TryParse(remaining.Value.TrimStart('/'), out statusCode))
                {
                    return true;
                }
            }

            statusCode = default;
            return false;
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
