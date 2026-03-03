using MediatR;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Commands;

/// <summary>
/// Handles CreateOrderCommand by delegating to IOrderService.
/// </summary>
public sealed class CreateOrderCommandHandler(IOrderService orderService)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var createRequest = new CreateOrderRequest(request.CustomerId, request.CurrencyCode, request.LineItems);
        return await orderService.CreateAsync(createRequest, cancellationToken);
    }
}
