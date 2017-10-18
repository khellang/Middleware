using System;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackException : Exception
    {
        private const string Fallback = nameof(SpaFallbackExtensions.UseSpaFallback);

        private const string StaticFiles = "UseStaticFiles";

        private const string Mvc = "UseMvc";

        public SpaFallbackException(PathString path) : this(GetMessage(path))
        {
        }

        public SpaFallbackException(string message) : base(message)
        {
        }

        private static string GetMessage(PathString path) => new StringBuilder()
            .AppendLine($"The {Fallback} middleware failed to provide a fallback response for path '{path}' because no middleware could handle it.")
            .AppendLine($"Make sure {Fallback} is placed before any middleware that is supposed to provide the fallback response. This is typically {StaticFiles} or {Mvc}.")
            .AppendLine($"If you're using {StaticFiles}, make sure the file exists on disk and that the middleware is configured correctly.")
            .AppendLine($"If you're using {Mvc}, make sure you have a controller and action method that can handle '{path}'.")
            .ToString();
    }
}
