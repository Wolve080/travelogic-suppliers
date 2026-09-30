using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Travelogic.Suppliers.Domain.Common;

namespace Travelogic.Suppliers.Api.Errors;

/// <summary>
/// Last line of defence: turns unhandled exceptions into problem details so clients never see a
/// stack trace, and makes sure every one is logged with the request's correlation id.
/// </summary>
internal sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            // Normally caught by the application layer; this is a safety net.
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            BadHttpRequestException bad => (bad.StatusCode, "The request could not be read."),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (499, "The client closed the request."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private partial void LogUnhandled(Exception exception, string method, string path);
}
