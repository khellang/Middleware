using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Hellang.Middleware.RateLimiting
{
    public delegate ValueTask<T> AsyncSelector<T>(HttpContext context);

    public delegate T Selector<out T>(HttpContext context);
}
