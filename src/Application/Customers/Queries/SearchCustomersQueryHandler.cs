using MediatR;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Queries;

/// <summary>
/// Handles SearchCustomersQuery by delegating to ICustomerService.
/// </summary>
public sealed class SearchCustomersQueryHandler(ICustomerService customerService)
    : IRequestHandler<SearchCustomersQuery, PaginatedList<CustomerDto>>
{
    public async Task<PaginatedList<CustomerDto>> Handle(SearchCustomersQuery request, CancellationToken cancellationToken)
    {
        return await customerService.SearchAsync(request.Search, request.Page, request.PageSize, cancellationToken);
    }
}
