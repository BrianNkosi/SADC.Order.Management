using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Customers.DTOs;

namespace SADC.Order.Management.Application.Customers;

public interface ICustomerService
{
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedList<CustomerDto>> SearchAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
