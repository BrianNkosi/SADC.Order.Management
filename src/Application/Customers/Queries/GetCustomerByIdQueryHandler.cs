using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers.Queries;

public sealed class GetCustomerByIdQueryHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    ILogger<GetCustomerByIdQueryHandler> logger)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving customer with Id={CustomerId}", request.Id);

        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer is null)
        {
            logger.LogWarning("Customer not found with Id={CustomerId}", request.Id);
            return null;
        }

        logger.LogInformation("Customer retrieved successfully with Id={CustomerId}", request.Id);
        return mapper.Map<CustomerDto>(customer);
    }
}
