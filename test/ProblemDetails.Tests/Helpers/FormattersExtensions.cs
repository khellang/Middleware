using Microsoft.Extensions.DependencyInjection;

namespace ProblemDetails.Tests.Helpers
{
    public static class FormattersExtensions
    {
        public static IMvcCoreBuilder AddFormatters(this IMvcCoreBuilder mvc)
        {
            return mvc.AddJsonOptions(json => { json.JsonSerializerOptions.IgnoreNullValues = true; })
                      .AddXmlDataContractSerializerFormatters();
        }
    }
}
