using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Application.Customers.Commands;

public sealed class CreateCustomerCommandHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    ILogger<CreateCustomerCommandHandler> logger)
    : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating customer: Name={CustomerName}, Email={CustomerEmail}, CountryCode={CountryCode}",
            request.Name, request.Email, request.CountryCode);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CountryCode = request.CountryCode.ToUpperInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Customer created: CustomerId={CustomerId}, Email={Email}", customer.Id, customer.Email);

        return mapper.Map<CustomerDto>(customer);
    }
}
