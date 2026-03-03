using MediatR;
using Microsoft.Extensions.Logging;

namespace SADC.Order.Management.Application.Common.Behaviours;

/// <summary>
/// Catches and logs any unhandled exceptions, then re-throws.
/// </summary>
public sealed class UnhandledExceptionLoggingBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogError(ex, "Unhandled exception for request {Name} {@Request}", requestName, request);
            throw;
        }
    }
}
