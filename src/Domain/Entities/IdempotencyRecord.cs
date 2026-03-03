namespace SADC.Order.Management.Domain.Entities;

/// <summary>
/// Stores idempotency keys to prevent duplicate request processing.
/// Each key is associated with the cached response payload so that
/// retried requests return the same result without re-executing side effects.
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; }

    /// <summary>
    /// The client-supplied idempotency key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Serialized response that was returned for the original request.
    /// </summary>
    public string ResponsePayload { get; set; } = string.Empty;

    /// <summary>
    /// When the idempotency record was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record expires and may be cleaned up (UTC).
    /// Retried requests after expiry are treated as new requests.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}
