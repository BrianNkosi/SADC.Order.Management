namespace SADC.Order.Management.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current correlation ID for the request scope.
/// Implemented in the API layer using HttpContext; can be stubbed in tests.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets or sets the correlation ID for the current scope.
    /// </summary>
    string CorrelationId { get; set; }
}
