using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Application.Customers;

public class CustomerService : ICustomerService
{
    private readonly IOrderManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCustomerRequest> _validator;

    public CustomerService(
        IOrderManagementDbContext context,
        IMapper mapper,
        IValidator<CreateCustomerRequest> validator)
    {
        _context = context;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CountryCode = request.CountryCode.ToUpperInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer is null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<PaginatedList<CustomerDto>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term));
        }

        query = query.OrderBy(c => c.Name);

        return await PaginatedList<CustomerDto>.CreateAsync(
            query.ProjectTo<CustomerDto>(_mapper.ConfigurationProvider),
            page,
            pageSize,
            cancellationToken);
    }
}
