using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Common.Models;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Entities;
using SADC.Order.Management.Domain.Enums;
using SADC.Order.Management.Domain.ValueObjects;

namespace SADC.Order.Management.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IOrderManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateOrderRequest> _validator;

    public OrderService(
        IOrderManagementDbContext context,
        IMapper mapper,
        IValidator<CreateOrderRequest> validator)
    {
        _context = context;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Verify customer exists and validate country-currency
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer '{request.CustomerId}' not found.");

        var currencyCode = request.CurrencyCode.ToUpperInvariant();
        if (!SadcCountryCurrency.IsValidCurrencyForCountry(customer.CountryCode, currencyCode))
        {
            var validCurrencies = SadcCountryCurrency.GetValidCurrencies(customer.CountryCode);
            throw new InvalidOperationException(
                $"Currency '{currencyCode}' is not valid for country '{customer.CountryCode}'. " +
                $"Valid currencies: {string.Join(", ", validCurrencies)}.");
        }

        var order = new Domain.Entities.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            CurrencyCode = currencyCode,
            CreatedAtUtc = DateTime.UtcNow,
            LineItems = request.LineItems.Select(li => new OrderLineItem
            {
                Id = Guid.NewGuid(),
                ProductSku = li.ProductSku,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList()
        };

        order.RecalculateTotal();

        // Atomic: insert order + outbox message in same transaction
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            Payload = JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
                order.CustomerId,
                order.CurrencyCode,
                order.TotalAmount,
                order.Status,
                order.CreatedAtUtc,
                LineItems = order.LineItems.Select(li => new
                {
                    li.Id,
                    li.ProductSku,
                    li.Quantity,
                    li.UnitPrice
                })
            }),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Order was created but could not be retrieved.");
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : _mapper.Map<OrderDto>(order);
    }

    public async Task<PaginatedList<OrderSummaryDto>> ListAsync(
        Guid? customerId, OrderStatus? status, int page, int pageSize,
        string? sortBy, bool descending, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        query = sortBy?.ToLowerInvariant() switch
        {
            "total" or "totalamount" => descending
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount),
            "status" => descending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => descending
                ? query.OrderByDescending(o => o.CreatedAtUtc)
                : query.OrderBy(o => o.CreatedAtUtc)
        };

        return await PaginatedList<OrderSummaryDto>.CreateAsync(
            query.ProjectTo<OrderSummaryDto>(_mapper.ConfigurationProvider),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<OrderDto> UpdateStatusAsync(
        Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order '{id}' not found.");

        // Idempotency: if already at the requested status, return current state
        if (order.Status == request.NewStatus)
            return _mapper.Map<OrderDto>(order);

        if (!order.TryTransitionTo(request.NewStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition order from '{order.Status}' to '{request.NewStatus}'. " +
                $"Allowed transitions: {string.Join(", ", order.Status.AllowedTransitions())}.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderDto>(order);
    }
}
