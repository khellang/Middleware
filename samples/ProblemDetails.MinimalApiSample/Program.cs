using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure problem details
builder.Services.AddProblemDetails(options =>
{
    // Only include exception details in a development environment. There's really no need
    // to set this as it's the default behavior. It's just included here for completeness :)
    options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();

    // This will map UserNotFoundException to the 404 Not Found status code and return custom problem details.
    options.Map<UserNotFoundException>(ex => new ProblemDetails
    {
        Title = "Could not find user",
        Status = StatusCodes.Status404NotFound,
        Detail = ex.Message,
    });

    // This will map NotImplementedException to the 501 Not Implemented status code.
    options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

    // You can configure the middleware to re-throw certain types of exceptions, all exceptions or based on a predicate.
    // This is useful if you have upstream middleware that  needs to do additional handling of exceptions.
    options.Rethrow<NotSupportedException>();

    // You can configure the middleware to ingore any exceptions of the specified type.
    // This is useful if you have upstream middleware that  needs to do additional handling of exceptions.
    // Note that unlike Rethrow, additional information will not be added to the exception.
    options.Ignore<DivideByZeroException>();

    // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
    // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
    options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

var app = builder.Build();

// Add ProblemDetailsMiddleware to the application pipeline
app.UseProblemDetails();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/maptodetail", () =>
{
    throw new UserNotFoundException();
});
app.MapGet("/maptostatus", () =>
{
    throw new NotImplementedException();
});
app.MapGet("/rethrow", () =>
{
    throw new NotSupportedException("Invalid operation");
});
app.MapGet("/ignore", () =>
{
    throw new DivideByZeroException();
});
app.MapGet("/error", () =>
{
    throw new Exception();
});
app.MapGet("/result", () =>
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

    return Results.BadRequest(problem);
});

app.Run();

public class OutOfCreditProblemDetails : ProblemDetails
{
    public OutOfCreditProblemDetails()
    {
        Accounts = new List<string>();
    }

    public decimal Balance { get; set; }

    public ICollection<string> Accounts { get; }
}

public class UserNotFoundException : Exception
{
}
