using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hellang.Middleware.ProblemDetails.Sample
{
    public class Startup
    {
        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        private IWebHostEnvironment Environment { get; }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseEnvironment(Environments.Development)
                //.UseEnvironment(Environments.Production) // Uncomment to remove exception details from responses.
                .ConfigureWebHostDefaults(web =>
                {
                    web.UseStartup<Startup>();
                });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProblemDetails(ConfigureProblemDetails)
                .AddControllers()
                    // Adds MVC conventions to work better with the ProblemDetails middleware.
                    .AddProblemDetailsConventions()
                .AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseProblemDetails();

            app.Use(CustomMiddleware);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ConfigureProblemDetails(ProblemDetailsOptions options)
        {
            // Only include exception details in a development environment. There's really no need
            // to set this as it's the default behavior. It's just included here for completeness :)
            options.IncludeExceptionDetails = (ctx, ex) => Environment.IsDevelopment();

            // Custom mapping function for FluentValidation's ValidationException.
            options.MapFluentValidationException();

            // You can configure the middleware to re-throw certain types of exceptions, all exceptions or based on a predicate.
            // This is useful if you have upstream middleware that needs to do additional handling of exceptions.
            options.Rethrow<NotSupportedException>();

            // This will map NotImplementedException to the 501 Not Implemented status code.
            options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

            // This will map HttpRequestException to the 503 Service Unavailable status code.
            options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);

            // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
            // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
            options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
        }

        private Task CustomMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.StartsWithSegments("/middleware", out _, out var remaining))
            {
                if (remaining.StartsWithSegments("/error"))
                {
                    throw new Exception("This is an exception thrown from middleware.");
                }

                if (remaining.StartsWithSegments("/status", out _, out remaining))
                {
                    var statusCodeString = remaining.Value.Trim('/');

                    if (int.TryParse(statusCodeString, out var statusCode))
                    {
                        context.Response.StatusCode = statusCode;
                        return Task.CompletedTask;
                    }
                }
            }

            return next();
        }
    }

    [Route("mvc")]
    [ApiController]
    public class MvcController : ControllerBase
    {
        [HttpGet("status/{statusCode}")]
        public ActionResult Status([FromRoute] int statusCode)
        {
            return StatusCode(statusCode);
        }

        [HttpGet("error")]
        public ActionResult Error()
        {
            throw new NotImplementedException("This is an exception thrown from an MVC controller.");
        }

        [HttpGet("modelstate")]
        public ActionResult InvalidModelState([Required, FromQuery] string asdf)
        {
            return Ok();
        }

        [HttpGet("error/details")]
        public ActionResult ErrorDetails()
        {
            ModelState.AddModelError("someProperty", "This property failed validation.");

            var validation = new ValidationProblemDetails(ModelState)
            {
                Status = StatusCodes.Status422UnprocessableEntity
            };

            throw new ProblemDetailsException(validation);
        }

        [HttpGet("detail")]
        public ActionResult<string> Detail()
        {
            return BadRequest("This will end up in the 'detail' field.");
        }

        [HttpGet("result")]
        public ActionResult<OutOfCreditProblemDetails> Result()
        {
            var problem = new OutOfCreditProblemDetails
            {
                Type = "https://example.com/probs/out-of-credit",
                Title = "You do not have enough credit.",
                Detail = "Your current balance is 30, but that costs 50.",
                Instance = "/account/12345/msgs/abc",
                Balance = 30.0m,
                Accounts = { "/account/12345", "/account/67890" }
            };

            return BadRequest(problem);
        }
    }

    public class OutOfCreditProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public OutOfCreditProblemDetails()
        {
            Accounts = new List<string>();
        }

        public decimal Balance { get; set; }

        public ICollection<string> Accounts { get; }
    }
}
