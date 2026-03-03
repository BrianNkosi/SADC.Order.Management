using MediatR;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Queries;

/// <summary>
/// Handles GetOrderByIdQuery by delegating to IOrderService.
/// </summary>
public sealed class GetOrderByIdQueryHandler(IOrderService orderService)
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await orderService.GetByIdAsync(request.Id, cancellationToken);
    }
}
