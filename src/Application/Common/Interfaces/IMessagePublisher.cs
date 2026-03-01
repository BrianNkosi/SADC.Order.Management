namespace SADC.Order.Management.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing messages to the message broker.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
        where T : class;
}
