using System.Threading.Tasks;

namespace Hellang.Middleware.RateLimiting
{
    internal static class SelectorExtensions
    {
        public static AsyncSelector<T> ToAsync<T>(this Selector<T> selector)
        {
            return ctx => new ValueTask<T>(selector(ctx));
        }
    }
}