namespace SADC.Order.Management.Application.Common.Interfaces;

/// <summary>
/// Marker interface for MediatR requests that carry a correlation ID.
/// The CorrelationIdBehaviour pipeline will auto-populate this from the accessor.
/// </summary>
public interface ICorrelatedRequest
{
    /// <summary>
    /// The correlation ID for distributed tracing. Auto-set by the pipeline.
    /// </summary>
    string CorrelationId { get; set; }
}
