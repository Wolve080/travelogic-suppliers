using System.Diagnostics;

namespace Travelogic.Suppliers.Api.Infrastructure;

/// <summary>
/// Propagates an <c>X-Correlation-ID</c> across service boundaries. Uses the caller's id when one is
/// sent (e.g. by an API gateway), otherwise the current trace id, and adds it to every log entry.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() is { Length: > 0 and <= MaxLength } supplied
            ? supplied
            : Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("correlation_id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}

internal static class CorrelationIdExtensions
{
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) ? value as string : null;
}
