using System;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.SpaFallback
{
    public class SpaFallbackOptions
    {
        public Func<HttpContext, PathString> FallbackPathFactory { get; set; } = _ => "/index.html";

        public bool AllowFileExtensions { get; set; } = false;

        public bool ThrowIfFallbackFails { get; set; } = true;
    }
}
