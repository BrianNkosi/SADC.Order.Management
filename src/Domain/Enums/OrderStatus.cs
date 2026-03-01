namespace SADC.Order.Management.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of an order.
/// Valid transitions:
///   Pending → Paid → Fulfilled
///   Pending → Cancelled
///   Paid → Cancelled
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Fulfilled = 2,
    Cancelled = 3
}

public static class OrderStatusExtensions
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Fulfilled, OrderStatus.Cancelled],
        [OrderStatus.Fulfilled] = [],
        [OrderStatus.Cancelled] = []
    };

    /// <summary>
    /// Checks whether transitioning from <paramref name="current"/> to <paramref name="next"/> is allowed.
    /// </summary>
    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
        => ValidTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);

    /// <summary>
    /// Returns the set of statuses reachable from the current status.
    /// </summary>
    public static IReadOnlySet<OrderStatus> AllowedTransitions(this OrderStatus current)
        => ValidTransitions.TryGetValue(current, out var allowed) ? allowed : new HashSet<OrderStatus>();
}
