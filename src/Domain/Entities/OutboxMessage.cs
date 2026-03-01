using SADC.Order.Management.Domain.Common;

namespace SADC.Order.Management.Domain.Entities;

/// <summary>
/// Transactional outbox message for reliable event publishing.
/// Written atomically with domain changes; polled and published to RabbitMQ by a background service.
/// </summary>
public class OutboxMessage : Entity
{
    /// <summary>
    /// The aggregate root type this event relates to (e.g. "Order").
    /// </summary>
    public string AggregateType { get; set; } = "Order";

    /// <summary>
    /// The aggregate root identifier this event relates to.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Fully-qualified event type name (e.g. "OrderCreated").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event payload (JSON).
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// When the domain event occurred (UTC).
    /// </summary>
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was created/enqueued (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Message version for consumer idempotency / deduplication.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the message was successfully published to the broker (UTC).
    /// Null means not yet processed.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Number of publish attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last error message if publish failed.
    /// </summary>
    public string? Error { get; set; }
}
