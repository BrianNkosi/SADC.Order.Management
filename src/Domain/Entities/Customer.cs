using SADC.Order.Management.Domain.Common;

namespace SADC.Order.Management.Domain.Entities;

/// <summary>
/// Represents a customer who can place orders.
/// </summary>
public class Customer : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code. Must be a valid SADC member state.
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
