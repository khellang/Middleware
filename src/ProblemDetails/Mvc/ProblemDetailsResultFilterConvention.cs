using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsResultFilterConvention : IActionModelConvention
    {
        private readonly ProblemDetailsResultFilterFactory _factory = new();

        public void Apply(ActionModel action)
        {
            action.Filters.Add(_factory);
        }
    }
}
