# Middleware

Various ASP.NET Core middleware. Mostly for use in APIs.

## ProblemDetails [![NuGet](https://img.shields.io/nuget/v/Hellang.Middleware.ProblemDetails.svg)](https://www.nuget.org/packages/Hellang.Middleware.ProblemDetails)

> Install-Package Hellang.Middleware.ProblemDetails

Configure in `Startup` class. See examples:

* [controller api sample](samples/ProblemDetails.Mvc.Sample/Program.cs)
* [minimal api sample](samples/ProblemDetails.MinimalApiSample/Program.cs)
* [asp.net core 6 sample app](https://github.com/christianacca/ProblemDetailsDemo)

Other packages that integrate with ProblemDetails middleware:

* [CcAcca.ApplicationInsights.ProblemDetails](https://www.nuget.org/packages/CcAcca.ApplicationInsights.ProblemDetails): Enriches Azure Application Insights request logging with ProblemDetails data

#### Logo

corrupted file by Rflor from the Noun Project

## SpaFallback [![NuGet](https://img.shields.io/nuget/v/Hellang.Middleware.SpaFallback.svg)](https://www.nuget.org/packages/Hellang.Middleware.SpaFallback)

> Install-Package Hellang.Middleware.SpaFallback

The `SpaFallback` middleware is designed to make your client-side SPA routing work seamlessly with your server-side routing.

When a request for a client-side route hits the server, chances are there's no middleware that will handle it. This means it will reach the end of the pipeline, the response status code will be set to 404 and the response will bubble back up the pipeline and be returned to the client. This is probably not what you want to happen.

Whenever a request can't be handled on the server, you usually want to fall back to your SPA and delegate the routing to the client. This is what the `SpaFallback` middleware enables.

The middleware works by passing all requests through the pipeline and let other middleware try to handle it. When the response comes back, it will perform a series of checks (outlined below) and optionally re-execute the pipeline, using the configured fallback path. This defaults to `/index.html`. This should bootstrap your SPA and let the client-side routing take over.

The following rules are verified before a fallback occurs:

 1. The method is **GET**.
 1. The status code is **404**.
 1. The response hasn't started yet.
 1. The requested path does not have a file extension.
 1. The request actually reached the end of the pipeline.

The middleware tries to be as smart as possible when determining whether a fallback should happen or not:

If the request path has a file extension, i.e. `/public/image.png`, the client probably wanted an actual file (typically served by [StaticFiles](https://github.com/aspnet/StaticFiles)), but it was missing from disk, so we let the 404 response through to the client.

In addition, we check that the response wasn't handled by other middleware (but still ended up with a 404 status code). This is useful if you want to prevent disclosing the existence of a resource that the client don't have access to. In order to achieve this, `AddSpaFallback` will automatically inject a "marker middleware" at the end of the pipeline. If the request reaches this middleware, it will set the response status code to 404 and add a tag to the `HttpContext.Items` dictionary. This tag is then checked in the fallback middleware to verify a "hard" 404.

### Usage

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSpaFallback();
        services.AddMvc();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSpaFallback();
        app.UseStaticFiles();
        app.UseMvc();
    }
}
```

## Authentication.JwtBearer.Google [![NuGet](https://img.shields.io/nuget/v/Hellang.Authentication.JwtBearer.Google.svg)](https://www.nuget.org/packages/Hellang.Authentication.JwtBearer.Google)

> Install-Package Hellang.Authentication.JwtBearer.Google

Makes it straight-forward to hook up authentication with Google identity tokens, using Microsoft's existing `Microsoft.AspNetCore.Authentication.JwtBearer` for parsing and validating the tokens.

### Usage

````csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x => x.UseGoogle(
        clientId: "<google-client-id>",
        hostedDomain: "<optional-g-suite-domain>"));
````

## Sponsors

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=khellang&utm_medium=Middleware) and [Dapper Plus](https://dapper-plus.net/?utm_source=khellang&utm_medium=Middleware) are major sponsors and proud to contribute to the development of Middleware.

[![Entity Framework Extensions](https://raw.githubusercontent.com/khellang/khellang/refs/heads/master/.github/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert?utm_source=khellang&utm_medium=Middleware)

[![Dapper Plus](https://raw.githubusercontent.com/khellang/khellang/refs/heads/master/.github/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert?utm_source=khellang&utm_medium=Middleware)
