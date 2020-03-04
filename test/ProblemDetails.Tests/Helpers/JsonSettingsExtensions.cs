using Microsoft.Extensions.DependencyInjection;

namespace ProblemDetails.Tests.Helpers
{
    public static class JsonSettingsExtensions
    {
        public static IMvcCoreBuilder AddJson(this IMvcCoreBuilder mvc)
        {
            return mvc.AddJsonOptions(json => json.JsonSerializerOptions.IgnoreNullValues = true);
        }
    }
}
