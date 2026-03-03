using MediatR;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Queries;

/// <summary>
/// Handles ListOrdersQuery by delegating to IOrderService.
/// </summary>
public sealed class ListOrdersQueryHandler(IOrderService orderService)
    : IRequestHandler<ListOrdersQuery, PaginatedList<OrderSummaryDto>>
{
    public async Task<PaginatedList<OrderSummaryDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        return await orderService.ListAsync(
            request.CustomerId,
            request.Status,
            request.Page,
            request.PageSize,
            request.SortBy,
            request.Descending,
            cancellationToken);
    }
}
