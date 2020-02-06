using Microsoft.Extensions.DependencyInjection;

#if (NETCOREAPP2_1 || NETCOREAPP2_2)
using Newtonsoft.Json;
#endif

namespace ProblemDetails.Tests.Helpers
{
    public static class JsonSettingsExtensions
    {
        public static IMvcCoreBuilder AddJson(this IMvcCoreBuilder mvc)
        {
#if (NETCOREAPP2_1 || NETCOREAPP2_2)
            return mvc.AddJsonFormatters(json => json.NullValueHandling = NullValueHandling.Ignore);
#else
            return mvc.AddJsonOptions(json => { json.JsonSerializerOptions.IgnoreNullValues = true; });
#endif
        }
    }
}