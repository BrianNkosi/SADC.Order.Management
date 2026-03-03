using MediatR;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Queries;

/// <summary>
/// Query to search customers with pagination.
/// </summary>
public sealed record SearchCustomersQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<CustomerDto>>;
