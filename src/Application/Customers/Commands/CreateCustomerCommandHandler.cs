using MediatR;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Commands;

/// <summary>
/// Handles CreateCustomerCommand by delegating to ICustomerService.
/// </summary>
public sealed class CreateCustomerCommandHandler(ICustomerService customerService)
    : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var createRequest = new CreateCustomerRequest(request.Name, request.Email, request.CountryCode);
        return await customerService.CreateAsync(createRequest, cancellationToken);
    }
}
