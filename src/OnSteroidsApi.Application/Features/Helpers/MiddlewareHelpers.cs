using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OnSteroidsApi.Application.Features.Helpers
{
    public static class MiddlewareHelpers
    {

        /// <summary>
        /// Custom response factory for model binding / deserialization errors.
        /// This ensures that ASP.NET Core returns errors in format of BaseErrorResponse class
        /// instead of the default ProblemDetails format.
        /// </summary>
        public static IActionResult ReturnValidationResponse(ActionContext context)
        {
            var errors = new List<string>();

            var modelState = context.ModelState;
            foreach (var kvp in modelState)
            {
                foreach (ModelError modelError in kvp.Value!.Errors)
                {
                    var propertyName = kvp.Key.Replace("$.", string.Empty);
                    errors.Add($"{propertyName}: {modelError.ErrorMessage}");
                }
            }

            (_, object error) = BaseResponseHelpers.ReturnValidationSyntaxError("Invalid value passed", errors);

            return new BadRequestObjectResult(error);
        }

    }
}
