using MediatR;
using SADC.Order.Management.Application.Orders.DTOs;

namespace SADC.Order.Management.Application.Orders.Queries;

/// <summary>
/// Query to get an order by ID, including line items.
/// </summary>
public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;
