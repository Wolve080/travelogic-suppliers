using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Travelogic.Suppliers.Application.Common;

namespace Travelogic.Suppliers.Api.Errors;

/// <summary>Maps application errors to RFC 9457 problem details responses.</summary>
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

        // A stable, machine readable code lets clients branch without parsing the message.
        problem.Extensions["code"] = error.Code;

        return new ProblemDetailsResult(problem);
    }

    /// <summary>
    /// Writes problem details as <c>application/problem+json</c>. A plain ObjectResult would have its
    /// content type overwritten by the controllers' [Produces("application/json")].
    /// </summary>
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
