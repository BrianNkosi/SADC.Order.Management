namespace SADC.Order.Management.Api.Middleware;

/// <summary>
/// Propagates or generates a correlation ID for distributed tracing.
/// Reads from "X-Correlation-Id" header; generates a new one if absent.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items["CorrelationId"] = correlationId.ToString();
        context.Response.Headers[CorrelationIdHeader] = correlationId.ToString();

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(context);
        }
    }
}
