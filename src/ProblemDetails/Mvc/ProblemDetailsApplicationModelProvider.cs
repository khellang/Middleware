using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails.Mvc
{
    internal class ProblemDetailsApplicationModelProvider : IApplicationModelProvider
    {
        public ProblemDetailsApplicationModelProvider()
        {
            ActionModelConventions = new List<IActionModelConvention>();

            var responseTypeAttribute = new ProducesErrorResponseTypeAttribute(typeof(MvcProblemDetails));
            ActionModelConventions.Add(new ApiConventionApplicationModelConvention(responseTypeAttribute));

            ActionModelConventions.Add(new ProblemDetailsResultFilterConvention());
        }

        public int Order => -1000 + 200;

        private List<IActionModelConvention> ActionModelConventions { get; }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controller in context.Result.Controllers)
            {
                if (!IsApiController(controller))
                {
                    continue;
                }

                foreach (var action in controller.Actions)
                {
                    foreach (var convention in ActionModelConventions)
                    {
                        convention.Apply(action);
                    }
                }
            }
        }

        private static bool IsApiController(ControllerModel controller)
        {
            if (controller.Attributes.OfType<IApiBehaviorMetadata>().Any())
            {
                return true;
            }

            var assembly = controller.ControllerType.Assembly;
            var attributes = assembly.GetCustomAttributes();

            return attributes.OfType<IApiBehaviorMetadata>().Any();
        }

        void IApplicationModelProvider.OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            // Not needed.
        }
    }
}
