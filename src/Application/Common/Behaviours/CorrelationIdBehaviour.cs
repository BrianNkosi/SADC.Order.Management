using MediatR;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;

namespace SADC.Order.Management.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that auto-populates the CorrelationId on any request
/// implementing <see cref="ICorrelatedRequest"/>, and enriches log context with it.
/// </summary>
public sealed class CorrelationIdBehaviour<TRequest, TResponse>(
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<CorrelationIdBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdAccessor.CorrelationId;

        if (request is ICorrelatedRequest correlated && string.IsNullOrEmpty(correlated.CorrelationId))
        {
            // Auto-populate the correlation ID on the command
            correlated.CorrelationId = correlationId;
        }

        var requestName = typeof(TRequest).Name;
        logger.LogInformation(
            "Dispatching {RequestName} with CorrelationId={CorrelationId}",
            requestName, correlationId);

        return await next();
    }
}
