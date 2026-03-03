using MediatR;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Commands;

/// <summary>
/// Command to create a new customer.
/// </summary>
public sealed record CreateCustomerCommand(
    string Name,
    string Email,
    string CountryCode) : IRequest<CustomerDto>, ICorrelatedRequest
{
    /// <inheritdoc />
    public string CorrelationId { get; set; } = string.Empty;
}
