using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Travelogic.Suppliers.Application.Common;

namespace Travelogic.Suppliers.Api.Errors;

internal static class ResultExtensions
{
    public static ActionResult Problem(this ControllerBase controller, Error error)
    {
        var factory = controller.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        ProblemDetails problem;
        if (error.Type == ErrorType.Validation && error.Details.Count > 0)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in error.Details)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            problem = factory.CreateValidationProblemDetails(controller.HttpContext, modelState, StatusCodes.Status400BadRequest, error.Message);
        }
        else
        {
            var status = error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };
            problem = factory.CreateProblemDetails(controller.HttpContext, status, detail: error.Message);
        }

        problem.Extensions["code"] = error.Code;

        return new ProblemDetailsResult(problem);
    }

    // ObjectResult would get its content type overwritten by [Produces("application/json")]
    private sealed class ProblemDetailsResult(ProblemDetails problem) : ActionResult
    {
        public override Task ExecuteResultAsync(ActionContext context)
        {
            var http = context.HttpContext;
            var json = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

            http.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            return http.Response.WriteAsJsonAsync(problem, problem.GetType(), json, "application/problem+json", http.RequestAborted);
        }
    }
}
