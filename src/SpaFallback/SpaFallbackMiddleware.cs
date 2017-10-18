using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackMiddleware
    {
        private const string MarkerKey = "SpaFallback";

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

            if (!ShouldFallback(context))
            {
                return;
            }

            var originalPath = context.Request.Path;

            try
            {
                var fallbackPath = Options.FallbackPathFactory?.Invoke(context);

                if (!fallbackPath.HasValue)
                {
                    throw new SpaFallbackException(
                        $"{nameof(Options.FallbackPathFactory)} must be specified and return a non-empty fallback path.");
                }

                context.Request.Path = fallbackPath.Value;

                await Next(context);

                if (ShouldThrow(context))
                {
                    // The fallback failed. Throw to let the developer know :)
                    throw new SpaFallbackException(fallbackPath.Value);
                }
            }
            finally
            {
                // Let's be nice and restore the original path...
                context.Request.Path = originalPath;
            }
        }

        private bool ShouldFallback(HttpContext context)
        {
            if (context.Response.HasStarted)
            {
                return false;
            }

            if (context.Response.StatusCode != StatusCodes.Status404NotFound)
            {
                return false;
            }

            // Fallback only on "hard" 404s, i.e. when the request reached the marker MW.
            if (!context.Items.ContainsKey(MarkerKey))
            {
                return false;
            }

            if (!HttpMethods.IsGet(context.Request.Method))
            {
                return false;
            }

            if (HasFileExtension(context.Request.Path))
            {
                return Options.AllowFileExtensions;
            }

            return true;
        }

        private bool ShouldThrow(HttpContext context)
        {
            return context.Response.StatusCode == StatusCodes.Status404NotFound && Options.ThrowIfFallbackFails;
        }

        private static bool HasFileExtension(PathString path)
        {
            return path.HasValue && Path.HasExtension(path.Value);
        }

        public class Marker
        {
            public Marker(RequestDelegate next)
            {
                Next = next;
            }

            private RequestDelegate Next { get; }

            public Task Invoke(HttpContext context)
            {
                context.Items[MarkerKey] = true; // Where the magic happens...
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }
        }
    }
}
