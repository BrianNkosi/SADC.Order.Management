using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace SADC.Order.Management.Application.Common.Behaviours;

/// <summary>
/// Pre-processor that logs every incoming request with its payload.
/// </summary>
public sealed class RequestLoggingBehaviour<TRequest>(ILogger<TRequest> logger)
    : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Processing request: {Name} {@Request}", requestName, request);
        return Task.CompletedTask;
    }
}
