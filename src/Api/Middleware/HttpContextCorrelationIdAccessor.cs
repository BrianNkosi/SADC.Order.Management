using SADC.Order.Management.Application.Common.Interfaces;

namespace SADC.Order.Management.Api.Middleware;

/// <summary>
/// Reads the correlation ID from HttpContext.Items, as set by CorrelationIdMiddleware.
/// Registered as scoped so it shares the same lifetime as the HTTP request.
/// </summary>
public sealed class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _correlationId;

    public HttpContextCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            if (_correlationId is not null)
                return _correlationId;

            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("CorrelationId", out var id) == true && id is string correlationId)
            {
                _correlationId = correlationId;
                return _correlationId;
            }

            // Fallback: generate one if not set (e.g., background worker scenarios)
            _correlationId = Guid.NewGuid().ToString();
            return _correlationId;
        }
        set => _correlationId = value;
    }
}
