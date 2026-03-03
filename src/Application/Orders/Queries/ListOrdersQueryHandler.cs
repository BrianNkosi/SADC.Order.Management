using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Queries;

public sealed class ListOrdersQueryHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    ILogger<ListOrdersQueryHandler> logger)
    : IRequestHandler<ListOrdersQuery, PaginatedList<OrderSummaryDto>>
{
    public async Task<PaginatedList<OrderSummaryDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Listing orders: CustomerId={CustomerId}, Status={Status}, Page={Page}, PageSize={PageSize}, SortBy={SortBy}, Descending={Descending}",
            request.CustomerId?.ToString() ?? "(all)", request.Status?.ToString() ?? "(all)",
            request.Page, request.PageSize, request.SortBy ?? "created", request.Descending);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(request.Page, 1);

        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .AsQueryable();

        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "total" or "totalamount" => request.Descending
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount),
            "status" => request.Descending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => request.Descending
                ? query.OrderByDescending(o => o.CreatedAtUtc)
                : query.OrderBy(o => o.CreatedAtUtc)
        };

        var result = await PaginatedList<OrderSummaryDto>.CreateAsync(
            query.ProjectTo<OrderSummaryDto>(mapper.ConfigurationProvider),
            page, pageSize, cancellationToken);

        logger.LogInformation("Order listing returned {TotalCount} result(s)", result.TotalCount);
        return result;
    }
}
