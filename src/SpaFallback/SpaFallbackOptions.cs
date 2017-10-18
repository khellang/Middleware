using System;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackOptions
    {
        private static readonly PathString DefaultFallbackPath = new PathString("/index.html");

        private static readonly Func<HttpContext, PathString> DefaultFallbackPathFactory = ctx => DefaultFallbackPath;

        public Func<HttpContext, PathString> FallbackPathFactory { get; set; } = DefaultFallbackPathFactory;

        public bool AllowFileExtensions { get; set; } = false;

        public bool ThrowIfFallbackFails { get; set; } = true;
    }
}
