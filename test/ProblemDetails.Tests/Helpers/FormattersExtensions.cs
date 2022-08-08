using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

namespace ProblemDetails.Tests.Helpers
{
    public static class FormattersExtensions
    {
        public static IMvcCoreBuilder AddFormatters(this IMvcCoreBuilder mvc)
        {
            return mvc.AddJsonOptions(ConfigureJson).AddXmlDataContractSerializerFormatters();
        }

        private static void ConfigureJson(JsonOptions json)
        {
#if NET6_0_OR_GREATER
            json.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
#else
            json.JsonSerializerOptions.IgnoreNullValues = true;
#endif
        }
    }
}
