using System;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackOptions
    {
        public bool AllowFileExtensions { get; set; } = false;

        public bool ThrowIfFallbackFails { get; set; } = true;

        public Func<HttpContext, PathString>? GetFallbackPath { get; set; }
    }
}
