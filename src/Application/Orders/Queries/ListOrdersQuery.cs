using MediatR;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Application.Orders.Queries;

/// <summary>
/// Query to list orders with optional filtering, sorting, and pagination.
/// </summary>
public sealed record ListOrdersQuery(
    Guid? CustomerId = null,
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool Descending = false) : IRequest<PaginatedList<OrderSummaryDto>>;
