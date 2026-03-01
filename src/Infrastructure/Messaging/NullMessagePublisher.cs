using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;

namespace SADC.Order.Management.Infrastructure.Messaging;

/// <summary>
/// No-op message publisher used when RabbitMQ is unavailable (e.g., local development).
/// Messages remain in the outbox table and will be published when RabbitMQ becomes available.
/// </summary>
public class NullMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NullMessagePublisher> _logger;

    public NullMessagePublisher(ILogger<NullMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogWarning("RabbitMQ is not available. Message to {Exchange}/{RoutingKey} queued in outbox only", exchange, routingKey);
        return Task.CompletedTask;
    }
}
