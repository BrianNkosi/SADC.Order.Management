using MediatR;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Commands;

/// <summary>
/// Command to create a new order with line items.
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string CurrencyCode,
    List<CreateOrderLineItemRequest> LineItems) : IRequest<OrderDto>, ICorrelatedRequest
{
    /// <inheritdoc />
    public string CorrelationId { get; set; } = string.Empty;
}
