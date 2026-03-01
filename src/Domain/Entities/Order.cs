using SADC.Order.Management.Domain.Common;
using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Domain.Entities;

/// <summary>
/// Represents a customer order with line items and lifecycle status.
/// </summary>
public class Order : AuditableEntity
{
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Current order status. Transitions are validated by <see cref="OrderStatusExtensions.CanTransitionTo"/>.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// ISO 4217 currency code. Must be valid for the customer's SADC country.
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Server-computed total: Σ(Quantity × UnitPrice) across all line items.
    /// Stored as decimal(18,2).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// Maps to SQL Server rowversion.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    // Navigation
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();

    /// <summary>
    /// Recalculates TotalAmount from line items.
    /// </summary>
    public void RecalculateTotal()
    {
        TotalAmount = LineItems.Sum(li => li.Quantity * li.UnitPrice);
    }

    /// <summary>
    /// Attempts to transition the order to a new status.
    /// </summary>
    /// <returns>True if transition was valid and applied.</returns>
    public bool TryTransitionTo(OrderStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
            return false;

        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }
}
