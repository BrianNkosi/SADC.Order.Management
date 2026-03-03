using MediatR;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Commands;

/// <summary>
/// Handles UpdateOrderStatusCommand by delegating to IOrderService.
/// </summary>
public sealed class UpdateOrderStatusCommandHandler(IOrderService orderService)
    : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var statusRequest = new UpdateOrderStatusRequest(request.NewStatus, request.IdempotencyKey);
        return await orderService.UpdateStatusAsync(request.OrderId, statusRequest, cancellationToken);
    }
}
