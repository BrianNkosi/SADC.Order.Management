using MediatR;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Queries;

/// <summary>
/// Handles GetCustomerByIdQuery by delegating to ICustomerService.
/// </summary>
public sealed class GetCustomerByIdQueryHandler(ICustomerService customerService)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        return await customerService.GetByIdAsync(request.Id, cancellationToken);
    }
}
