using SADC.Order.Management.Domain.Common;

namespace SADC.Order.Management.Domain.Entities;

/// <summary>
/// Represents a single line item within an order.
/// </summary>
public class OrderLineItem : Entity
{
    public Guid OrderId { get; set; }

    /// <summary>
    /// Stock-keeping unit identifier for the product.
    /// </summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered. Must be greater than 0.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit. Must be ≥ 0. Stored as decimal(18,2).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Computed sub-total for this line item.
    /// </summary>
    public decimal LineTotal => Quantity * UnitPrice;

    // Navigation
    public Order Order { get; set; } = null!;
}
