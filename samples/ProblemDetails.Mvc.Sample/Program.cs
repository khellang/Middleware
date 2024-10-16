global using ProblemDetailsOptions = Hellang.Middleware.ProblemDetails.ProblemDetailsOptions;
using Hellang.Middleware.ProblemDetails;
using Hellang.Middleware.ProblemDetails.Mvc;
using ProblemDetails.Mvc.Sample;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseEnvironment(Environments.Development);
// Uncomment line below to remove exception details from responses.
//builder.Host.UseEnvironment(Environments.Production);

// Add services to the container.
builder.Services
    .AddProblemDetails(options =>
    {
        // Only include exception details in a development environment. There's really no need
        // to set this as it's the default behavior. It's just included here for completeness :)
        options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();

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

    })
    .AddControllersWithViews()
    .AddProblemDetailsConventions()
    .AddJsonOptions(x => x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseProblemDetails();
app.Use(CustomMiddleware);
app.UseRouting();
app.MapControllers();
app.Run();

static Task CustomMiddleware(HttpContext context, Func<Task> next)
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

