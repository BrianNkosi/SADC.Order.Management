namespace SADC.Order.Management.Domain.Common;

/// <summary>
/// Base entity with typed identifier.
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Convenience base entity with Guid id.
/// </summary>
public abstract class Entity : Entity<Guid>
{
}
