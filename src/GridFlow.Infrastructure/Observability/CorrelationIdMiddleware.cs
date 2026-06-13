using Microsoft.AspNetCore.Http;

using Serilog.Context;

namespace GridFlow.Infrastructure.Observability;

/// <summary>
/// Reads or generates <see cref="CorrelationIds.HeaderName"/> for each HTTP request, echoes it on the
/// response, and pushes it into the Serilog log context for the remainder of the pipeline.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIds.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Response.Headers[CorrelationIds.HeaderName] = correlationId;

        using (LogContext.PushProperty(CorrelationIds.LogPropertyName, correlationId))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}