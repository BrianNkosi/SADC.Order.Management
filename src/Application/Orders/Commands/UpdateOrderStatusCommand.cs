using MediatR;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Application.Orders.Commands;

/// <summary>
/// Command to update an order's status with validated transitions.
/// </summary>
public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    string? IdempotencyKey = null) : IRequest<OrderDto>, ICorrelatedRequest, IIdempotentCommand
{
    /// <inheritdoc />
    public string CorrelationId { get; set; } = string.Empty;
}
