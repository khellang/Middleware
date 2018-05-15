using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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

        private static TestServer CreateServer()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(x => x
                    .AddCors()
                    .AddMvcCore()
                    .AddJsonFormatters(ConfigureJson))
                .Configure(x => x
                    .UseCors(y => y.AllowAnyOrigin())
                    .UseProblemDetails()
                    .Use(HandleRoutes));

            return new TestServer(builder);
        }

        private static Task HandleRoutes(HttpContext context, Func<Task> next)
        {
            if (TryGetStatusCode(context, "/success", out var statusCode))
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }

            if (TryGetStatusCode(context, "/error", out statusCode))
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

            if (context.Request.Path.StartsWithSegments("/exception"))
            {
                throw new Exception("Request Failed");
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
