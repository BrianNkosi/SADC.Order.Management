using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Queries;

public sealed class SearchCustomersQueryHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    ILogger<SearchCustomersQueryHandler> logger)
    : IRequestHandler<SearchCustomersQuery, PaginatedList<CustomerDto>>
{
    public async Task<PaginatedList<CustomerDto>> Handle(SearchCustomersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Searching customers: Search={SearchTerm}, Page={Page}, PageSize={PageSize}",
            request.Search ?? "(all)", request.Page, request.PageSize);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(request.Page, 1);

        var query = context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term));
        }

        query = query.OrderBy(c => c.Name);

        var result = await PaginatedList<CustomerDto>.CreateAsync(
            query.ProjectTo<CustomerDto>(mapper.ConfigurationProvider),
            page, pageSize, cancellationToken);

        logger.LogInformation("Customer search returned {TotalCount} result(s)", result.TotalCount);
        return result;
    }
}
