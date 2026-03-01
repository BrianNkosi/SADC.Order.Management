using SADC.Order.Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OrderEntity = SADC.Order.Management.Domain.Entities.Order;

namespace SADC.Order.Management.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext for the application layer.
/// </summary>
public interface IOrderManagementDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<OrderEntity> Orders { get; }
    DbSet<OrderLineItem> OrderLineItems { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
