using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Application.Orders.Commands;

public sealed class UpdateOrderStatusCommandHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    ILogger<UpdateOrderStatusCommandHandler> logger)
    : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Updating order status: OrderId={OrderId}, NewStatus={NewStatus}",
            request.OrderId, request.NewStatus);

        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order not found: OrderId={OrderId}", request.OrderId);
            throw new KeyNotFoundException($"Order '{request.OrderId}' not found.");
        }

        if (order.Status == request.NewStatus)
        {
            logger.LogInformation(
                "Order {OrderId} already at status {OrderStatus}, returning current state",
                request.OrderId, order.Status);
            return mapper.Map<OrderDto>(order);
        }

        if (!order.TryTransitionTo(request.NewStatus))
        {
            logger.LogWarning(
                "Invalid status transition: OrderId={OrderId}, {CurrentStatus} -> {NewStatus}. Allowed: {AllowedTransitions}",
                request.OrderId, order.Status, request.NewStatus,
                string.Join(", ", order.Status.AllowedTransitions()));
            throw new InvalidOperationException(
                $"Cannot transition order from '{order.Status}' to '{request.NewStatus}'. " +
                $"Allowed transitions: {string.Join(", ", order.Status.AllowedTransitions())}.");
        }

        await context.SaveChangesAsync(cancellationToken);

        var result = mapper.Map<OrderDto>(order);

        logger.LogInformation(
            "Order status updated: OrderId={OrderId}, Status={OrderStatus}",
            result.Id, result.Status);

        return result;
    }
}
