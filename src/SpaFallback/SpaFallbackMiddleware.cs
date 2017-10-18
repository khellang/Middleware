using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackMiddleware
    {
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
                var fallbackPath = Options.FallbackPathFactory?.Invoke(context);

                if (!fallbackPath.HasValue)
                {
                    throw new SpaFallbackException(
                        $"{nameof(Options.FallbackPathFactory)} must be specified and return a non-empty fallback path.");
                }

                context.Request.Path = fallbackPath.Value;

                await Next(context);

                if (context.ShouldThrow(Options))
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
    }
}
