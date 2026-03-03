using MediatR;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Queries;

/// <summary>
/// Query to get a customer by ID.
/// </summary>
public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto?>;
