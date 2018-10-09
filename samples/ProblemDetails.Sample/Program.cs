using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Hellang.Middleware.ProblemDetails.Sample
{
    public class Startup : StartupBase
    {
        public Startup(IHostingEnvironment environment)
        {
            Environment = environment;
        }

        private IHostingEnvironment Environment { get; }

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseEnvironment(EnvironmentName.Development)
                //.UseEnvironment(EnvironmentName.Production) // Uncomment to remove exception details from responses.
                .UseStartup<Startup>();
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore().AddJsonFormatters(x => x.NullValueHandling = NullValueHandling.Ignore);
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseProblemDetails(x =>
            {
                // This is the default behavior; only include exception details in a development environment.
                x.IncludeExceptionDetails = ctx => Environment.IsDevelopment();

                // This will map NotImplementedException to the 501 Not Implemented status code.
                x.Map<NotImplementedException>(ex => new ExceptionProblemDetails(ex, StatusCodes.Status501NotImplemented));
            });

            app.Use(CustomMiddleware);

            app.UseMvc();
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
    public class MvcController : ControllerBase
    {
        [HttpGet("status/{statusCode}")]
        public IActionResult Status([FromRoute] int statusCode)
        {
            return StatusCode(statusCode);
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            throw new NotImplementedException("This is an exception thrown from an MVC controller.");
        }

        [HttpGet("error/details")]
        public IActionResult ErrorDetails()
        {
            ModelState.AddModelError("someProperty", "This property failed validation.");

            var validation = new ValidationProblemDetails(ModelState)
            {
                Status = StatusCodes.Status422UnprocessableEntity
            };

            throw new ProblemDetailsException(validation);
        }

        [HttpGet("result")]
        public IActionResult Result()
        {
            var problem = new OutOfCreditProblemDetails
            {
                Type = "https://example.com/probs/out-of-credit",
                Title = "You do not have enough credit.",
                Detail = "Your current balance is 30, but that costs 50.",
                Instance = "/account/12345/msgs/abc",
                Balance = 30.0m,
                Accounts = { "/account/12345","/account/67890" }
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
