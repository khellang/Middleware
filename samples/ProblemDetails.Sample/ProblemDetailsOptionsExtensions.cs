using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Hellang.Middleware.ProblemDetails.Sample
{
    public static class ProblemDetailsOptionsExtensions
    {
        public static void MapFluentValidationException(this ProblemDetailsOptions options) =>
            options.Map<ValidationException>((ctx, ex) =>
            {
                var factory = ctx.RequestServices.GetRequiredService<ProblemDetailsFactory>();

                var errors = ex.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Select(x => x.ErrorMessage).ToArray());

                return factory.CreateValidationProblemDetails(ctx, errors);
            });
    }
}
