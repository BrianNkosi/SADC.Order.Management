using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Queries;

public sealed class GetOrderByIdQueryHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    ILogger<GetOrderByIdQueryHandler> logger)
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order not found with Id={OrderId}", request.Id);
            return null;
        }

        logger.LogInformation("Order retrieved: OrderId={OrderId}, Status={OrderStatus}", order.Id, order.Status);
        return mapper.Map<OrderDto>(order);
    }
}
