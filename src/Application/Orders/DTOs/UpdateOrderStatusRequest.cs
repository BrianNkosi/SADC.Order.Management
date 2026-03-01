using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Application.Orders.DTOs;

public record UpdateOrderStatusRequest(
    OrderStatus NewStatus,
    string? IdempotencyKey = null);
