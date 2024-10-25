using Hellang.Middleware.SpaFallback;
using Microsoft.Extensions.FileProviders;

var folder = Path.Combine(AppContext.BaseDirectory, "StaticFiles");
string? requestPath = null;
var options = new StaticFileOptions
{
    RequestPath = requestPath,
    FileProvider = new PhysicalFileProvider(folder)
};

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSpaFallback(y => y.AllowFileExtensions = true);

var app = builder.Build();
app.UseSpaFallback().UseStaticFiles(options).Use(HandleRoutes);
app.Run();

Task HandleRoutes(HttpContext context, Func<Task> next)
{
    if (context.Request.Path.StartsWithSegments("/soft-404"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }

    if (context.Request.Path.StartsWithSegments("/no-content"))
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }

    return next();
}
