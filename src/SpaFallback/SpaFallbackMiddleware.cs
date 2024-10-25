using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackMiddleware
    {
        private static readonly PathString DefaultFallbackPath = new PathString("/index.html");

        public SpaFallbackMiddleware(RequestDelegate next, IOptions<SpaFallbackOptions> options)
        {
            Next = next;
            Options = options.Value;
        }

        private RequestDelegate Next { get; }

        private SpaFallbackOptions Options { get; }

        public async Task Invoke(HttpContext context)
        {
            await Next(context);

            if (context.ShouldFallback(Options))
            {
                await Fallback(context);
            }
        }

        private async Task Fallback(HttpContext context)
        {
            var originalPath = context.Request.Path;

            try
            {
                var fallbackPath = GetFallbackPath(context);

                context.Request.Path = fallbackPath;

                // Reset HTTP response headers, status code and the response body to make room for the redirected endpoint content.
                context.Response.Clear();

                await Next(context);

                if (context.ShouldThrow(Options))
                {
                    // The fallback failed. Throw to let the developer know :)
                    throw new SpaFallbackException(fallbackPath);
                }
            }
            finally
            {
                // Let's be nice and restore the original path...
                context.Request.Path = originalPath;
            }
        }

        private PathString GetFallbackPath(HttpContext context)
        {
            return Options.GetFallbackPath?.Invoke(context) ?? DefaultFallbackPath;
        }
    }
}
