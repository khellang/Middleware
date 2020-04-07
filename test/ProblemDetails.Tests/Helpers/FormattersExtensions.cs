using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
            json.JsonSerializerOptions.IgnoreNullValues = true;
        }
    }
}
