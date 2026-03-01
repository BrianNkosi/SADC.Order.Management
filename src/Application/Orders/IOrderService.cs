using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Application.Orders;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedList<OrderSummaryDto>> ListAsync(
        Guid? customerId, OrderStatus? status, int page, int pageSize,
        string? sortBy, bool descending, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
}
