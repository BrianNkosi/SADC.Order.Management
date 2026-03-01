namespace SADC.Order.Management.Domain.Common;

/// <summary>
/// Base class for entities that track creation and modification timestamps.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
