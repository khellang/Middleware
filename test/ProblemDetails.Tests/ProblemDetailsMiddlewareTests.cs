using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Hellang.Middleware.ProblemDetails.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProblemDetails.Tests.Helpers;
using Xunit;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ProblemDetails.Tests
{
    public class ProblemDetailsMiddlewareTests
    {
        private const string ProblemJsonMediaType = "application/problem+json";

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
            using var client = CreateClient(handler: ResponseWithStatusCode(expected), SetOnBeforeWriteDetails);

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(expected, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Exception_IsHandled()
        {
            using var client = CreateClient(handler: ResponseThrows(), SetOnBeforeWriteDetails);

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: true);
        }

        [Theory]
        [InlineData("include")]
        [InlineData("exlude")]
        public async Task Exception_MapPredicate(string predicateData)
        {
            var testExceptionData = "include";

            using var client = CreateClient(
                handler: ResponseThrows(new ArgumentException(string.Empty, testExceptionData)),
                configureOptions: options =>
                {
                    options.Map<ArgumentException>(
                        (ctx, ex) => ex.ParamName == predicateData,
                        (ctx, ex) => new MvcProblemDetails()
                        {
                            Status = (int)HttpStatusCode.BadRequest,
                        });

                    options.Map<Exception>((ctx, ex) => new MvcProblemDetails()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                    });
                });

            var response = await client.GetAsync(string.Empty);

            if (testExceptionData == predicateData)
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData("application/csv", "application/problem+json")]
        [InlineData("application/json", "application/problem+json")]
        public async Task ContentTypes_Default_Options(string requestAcceptContentType, string responseContentType)
        {
            using var client = CreateClient(handler: ResponseThrows(), options => options.Map<Exception>(_ => new MvcProblemDetails()));

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd(requestAcceptContentType);

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(responseContentType, response.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData("application/problem+json", "application/json", "application/problem+json")]
        [InlineData("application/problem+json", "application/xml", "application/problem+json")]
        [InlineData("application/problem+json", "application/csv", "application/problem+json")]
        public async Task ContentTypes_Custom_Options(string optionsContentType, string requestAcceptContentType, string responseContentType)
        {
            void Configure(ProblemDetailsOptions options)
            {
                options.ContentTypes.Clear();
                options.ContentTypes.Add(optionsContentType);
                options.Map<Exception>(_ => new MvcProblemDetails());
            }

            using var client = CreateClient(handler: ResponseThrows(), Configure);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd(requestAcceptContentType);

            var response = await client.GetAsync(string.Empty);

            Assert.Equal(responseContentType, response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task ProblemDetailsException_IsHandled()
        {
            const int expected = StatusCodes.Status429TooManyRequests;

            var details = new MvcProblemDetails
            {
                Title = ReasonPhrases.GetReasonPhrase(expected),
                Type = $"https://httpstatuses.com/{expected}",
                Status = expected,
            };

            var ex = new ProblemDetailsException(details);

            using var client = CreateClient(handler: ResponseThrows(ex), SetOnBeforeWriteDetails);

            var response = await client.GetAsync("/");

            Assert.Equal(expected, (int)response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Catchall_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            using var client = CreateClient(handler: ResponseThrows());

            var response = await client.GetAsync(string.Empty);

            AssertUnhandledExceptionLogged(Logger);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Mapped_Server_Exception_Is_Logged_As_Unhandled_Error()
        {
            static void Configure(ProblemDetailsOptions options)
            {
                options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
            }

            var handler = ResponseThrows<NotImplementedException>();

            using var client = CreateClient(handler, Configure);

            var response = await client.GetAsync("/");

            AssertUnhandledExceptionLogged(Logger);
            Assert.Equal((HttpStatusCode)StatusCodes.Status501NotImplemented, response.StatusCode);
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

            AssertUnhandledExceptionNotLogged(Logger);
            Assert.Equal((HttpStatusCode)StatusCodes.Status429TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task Mapped_Client_Exception_Is_Not_Logged_As_Unhandled_Error()
        {
            static void Configure(ProblemDetailsOptions options)
            {
                options.MapToStatusCode<NotImplementedException>(StatusCodes.Status403Forbidden);
            }

            var handler = ResponseThrows(new NotImplementedException());

            using var client = CreateClient(handler, Configure);

            var response = await client.GetAsync(string.Empty);

            AssertUnhandledExceptionNotLogged(Logger);
            Assert.Equal((HttpStatusCode)StatusCodes.Status403Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("Staging", false)]
        [InlineData("Production", false)]
        [InlineData("Development", true)]
        public async Task ExceptionDetails_AreOnlyIncludedInDevelopment(string environment, bool expectExceptionDetails)
        {
            using var client = CreateClient(handler: ResponseThrows(), environment: environment);

            var response = await client.GetAsync(string.Empty);

            var content = await response.Content.ReadFromJsonAsync<MvcProblemDetails>();

            Assert.Equal(expectExceptionDetails, content.Extensions.ContainsKey("errors"));
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

            var request = new HttpRequestMessage(HttpMethod.Options, "/");

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
            static Task WriteResponse(HttpContext context)
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
            static async Task WriteResponse(HttpContext context)
            {
                await context.Response.WriteAsync("hello");
                throw new InvalidOperationException("Request Failed");
            }

            using var client = CreateClient(handler: WriteResponse);

            await AssertThrowsInnerAsync<InvalidOperationException>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException_WhenExceptionIsMappedToNull()
        {
            using var client = CreateClient(handler: ResponseThrows<DivideByZeroException>(), options =>
            {
                options.Map<DivideByZeroException>(ex => null);
            });

            await Assert.ThrowsAsync<DivideByZeroException>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException()
        {
            var ex = new ProblemDetailsException(new EvilProblemDetails());

            using var client = CreateClient(handler: ResponseThrows(ex));

            await AssertThrowsInnerAsync<ProblemDetailsException>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException_RethrowIsIntended()
        {
            using var client = CreateClient(handler: ResponseThrows<DivideByZeroException>(), options =>
            {
                options.Rethrow<DivideByZeroException>();
            });

            await AssertThrowsInnerAsync<DivideByZeroException>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException_RethrowIsIntendedAndExceptionDerived()
        {
            using var client = CreateClient(handler: ResponseThrows<DivideByZeroException>(), options =>
            {
                options.Rethrow<Exception>();
            });

            await AssertThrowsInnerAsync<DivideByZeroException>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_RethrowsException_RethrowIsIntendedAndPredicate()
        {
            using var client = CreateClient(handler: ResponseThrows<DivideByZeroException>(), options =>
            {
                options.Rethrow<DivideByZeroException>((ctx, ex ) => true);
            });

            await AssertThrowsInnerAsync<DivideByZeroException>(() => client.GetAsync(string.Empty));
        }

        [Fact]
        public async Task ProblemDetailsExceptionHandler_CatchException_RethrowIsUnintended()
        {
            using var client = CreateClient(handler: ResponseThrows(new InvalidCastException("A")), options =>
            {
                options.Rethrow<DivideByZeroException>();
                options.Rethrow<InvalidCastException>((context, ex) => ex.Message != "A");
            });

            await client.GetAsync(string.Empty);
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

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotImplemented)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task Mvc_ErrorStatusCode_IsHandled(HttpStatusCode expected)
        {
            using var client = CreateClient(configureOptions: SetOnBeforeWriteDetails);

            var response = await client.GetAsync($"mvc/statusCode/{(int)expected}");

            Assert.Equal(expected, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Mvc_Exception_IsHandled()
        {
            using var client = CreateClient(configureOptions: SetOnBeforeWriteDetails);

            var response = await client.GetAsync("mvc/error");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: true);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.UnprocessableEntity)]
        public async Task Mvc_InvalidModelState_IsHandled(HttpStatusCode statusCode)
        {
            using var client = CreateClient(configureOptions: SetValidationStatusCode(statusCode));

            var response = await client.GetAsync("mvc/statusCode");

            Assert.Equal(statusCode, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Mvc_StringDetail_IsHandled()
        {
            using var client = CreateClient(configureOptions: SetOnBeforeWriteDetails);

            var response = await client.GetAsync("mvc/string-detail");

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Mvc_ProblemModel_IsHandled()
        {
            using var client = CreateClient(configureOptions: SetOnBeforeWriteDetails);

            var response = await client.GetAsync("mvc/problem-model");

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Mvc_ModelStateDictionary_IsHandled()
        {
            using var client = CreateClient(configureOptions: SetOnBeforeWriteDetails);

            var response = await client.GetAsync("mvc/validation");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: false);
        }

        [Fact]
        public async Task Mvc_ErrorModel_IsHandled()
        {
            using var client = CreateClient(configureOptions: SetOnBeforeWriteDetails);

            var response = await client.GetAsync("mvc/error-model");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            await AssertIsProblemDetailsResponse(response, expectExceptionDetails: true);
        }

        private static async Task AssertIsProblemDetailsResponse(HttpResponseMessage response, bool expectExceptionDetails)
        {
            Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType.MediaType);

            var content = await response.Content.ReadFromJsonAsync<MvcProblemDetails>();

            Assert.NotNull(content);

            Assert.NotEmpty(content.Type);
            Assert.NotEmpty(content.Title);
            Assert.NotNull(content.Status);

            if (expectExceptionDetails)
            {
                Assert.True(content.Extensions.ContainsKey("errors"), "Expected response to contain exception details.");
            }

            Assert.Contains(nameof(ProblemDetailsOptions.OnBeforeWriteDetails), content.Extensions.Keys);
        }

        private static void AssertUnhandledExceptionLogged(InMemoryLogger<ProblemDetailsMiddleware> logger)
        {
            Assert.Single(logger.Messages.Where(m => m.Type == LogLevel.Error));
        }

        private static void AssertUnhandledExceptionNotLogged(InMemoryLogger<ProblemDetailsMiddleware> logger)
        {
            Assert.Empty(logger.Messages.Where(m => m.Type == LogLevel.Error));
        }

        private static async Task AssertThrowsInnerAsync<T>(Func<Task> testCode)
        {
            Assert.IsType<T>(GetInnermostException(await Record.ExceptionAsync(testCode)));
        }

        private static Exception GetInnermostException(Exception exception)
        {
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }

        private static Action<ProblemDetailsOptions> SetValidationStatusCode(HttpStatusCode statusCode)
        {
            return options =>
            {
                options.ValidationProblemStatusCode = (int)statusCode;
                SetOnBeforeWriteDetails(options);
            };
        }

        private static void SetOnBeforeWriteDetails(ProblemDetailsOptions options)
        {
            options.OnBeforeWriteDetails = (_, details) =>
            {
                details.Extensions[nameof(ProblemDetailsOptions.OnBeforeWriteDetails)] = true;
            };
        }

        private HttpClient CreateClient(RequestDelegate handler = null, Action<ProblemDetailsOptions> configureOptions = null, string environment = null)
        {
            var builder = new WebHostBuilder()
                .UseEnvironment(environment ?? Environments.Development)
                .ConfigureServices(x => x
                    .AddSingleton<ILogger<ProblemDetailsMiddleware>>(Logger)
                    .AddProblemDetails(configureOptions)
                    .AddCors()
                    .AddControllers()
                        .AddProblemDetailsConventions())
                .Configure(x => x
                    .UseCors(y => y.AllowAnyOrigin())
                    .UseProblemDetails()
                    .UseRouting()
                    .UseEndpoints(y =>
                    {
                        y.MapControllers();
                        if (handler != null)
                        {
                            y.MapGet("/", handler);
                        }
                    }));

            var server = new TestServer(builder);

            var client = server.CreateClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd(ProblemJsonMediaType);

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

        private static RequestDelegate ResponseThrows<TException>() where TException : Exception, new()
        {
            return ResponseThrows(new TException());
        }

        private static RequestDelegate ResponseThrows(Exception exception = null)
        {
            return context => Task.FromException(exception ?? new InvalidOperationException("Request failed."));
        }

        private class EvilProblemDetails : MvcProblemDetails
        {
            public string EvilProperty => throw new InvalidOperationException("This should throw during serialization.");
        }
    }

    [Route("mvc")]
    [ApiController]
    public class Controller : ControllerBase
    {
        [HttpGet("statusCode/{statusCode?}")]
        public ActionResult Status([Required] int? statusCode)
        {
            return StatusCode(statusCode.Value);
        }

        [HttpGet("validation")]
        public ActionResult Validation()
        {
            ModelState.AddModelError("error", "This is an error.");
            return BadRequest(ModelState);
        }

        [HttpGet("error")]
        public ActionResult Error()
        {
            throw new Exception("BOOM!");
        }

        [HttpGet("string-detail")]
        public ActionResult StringDetail()
        {
            return UnprocessableEntity("Some details.");
        }

        [HttpGet("problem-model")]
        public ActionResult ProblemModel()
        {
            var details = new MvcProblemDetails()
            {
                Type = ReasonPhrases.GetReasonPhrase(StatusCodes.Status429TooManyRequests),
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Take a breath, relax.",
            };

            return new ObjectResult(details);
        }

        [HttpGet("error-model")]
        public ActionResult ErrorModel()
        {
            return BadRequest(new Exception("Hello"));
        }
    }
}
