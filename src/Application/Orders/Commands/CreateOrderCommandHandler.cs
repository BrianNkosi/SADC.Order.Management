using System.Text.Json;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Entities;
using SADC.Order.Management.Domain.Enums;
using SADC.Order.Management.Domain.ValueObjects;
using OrderEntity = SADC.Order.Management.Domain.Entities.Order;

namespace SADC.Order.Management.Application.Orders.Commands;

public sealed class CreateOrderCommandHandler(
    IOrderManagementDbContext context,
    IMapper mapper,
    IValidator<CreateOrderRequest> validator,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<CreateOrderCommandHandler> logger)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order creation started: CustomerId={CustomerId}, CurrencyCode={CurrencyCode}, LineItemCount={LineItemCount}",
            request.CustomerId, request.CurrencyCode, request.LineItems.Count);

        // Step 1: Validate
        var createRequest = new CreateOrderRequest(request.CustomerId, request.CurrencyCode, request.LineItems);
        var validationResult = await validator.ValidateAsync(createRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning(
                "Order validation failed for CustomerId={CustomerId}: {ValidationErrors}",
                request.CustomerId,
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            throw new ValidationException(validationResult.Errors);
        }

        // Step 2: Verify customer exists
        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            logger.LogWarning("Customer not found: CustomerId={CustomerId}", request.CustomerId);
            throw new KeyNotFoundException($"Customer '{request.CustomerId}' not found.");
        }

        // Step 3: Validate country-currency combination
        var currencyCode = request.CurrencyCode.ToUpperInvariant();
        if (!SadcCountryCurrency.IsValidCurrencyForCountry(customer.CountryCode, currencyCode))
        {
            var validCurrencies = SadcCountryCurrency.GetValidCurrencies(customer.CountryCode);
            logger.LogWarning(
                "Invalid currency {CurrencyCode} for country {CountryCode}. Valid: {ValidCurrencies}",
                currencyCode, customer.CountryCode, string.Join(", ", validCurrencies));
            throw new InvalidOperationException(
                $"Currency '{currencyCode}' is not valid for country '{customer.CountryCode}'. " +
                $"Valid currencies: {string.Join(", ", validCurrencies)}.");
        }

        // Step 4: Build order entity
        var order = new OrderEntity
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

        // Step 5: Create outbox message with correlation ID for distributed tracing
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Order",
            AggregateId = order.Id,
            Type = "OrderCreated",
            Payload = JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
                order.CustomerId,
                order.CurrencyCode,
                order.TotalAmount,
                order.Status,
                order.CreatedAtUtc,
                CorrelationId = correlationIdAccessor.CorrelationId,
                LineItems = order.LineItems.Select(li => new
                {
                    li.Id,
                    li.ProductSku,
                    li.Quantity,
                    li.UnitPrice
                })
            }),
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Version = 1
        };

        // Step 6: Persist atomically
        context.Orders.Add(order);
        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync(cancellationToken);

        // Reload with related data for full mapping
        var result = await context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Order was created but could not be retrieved.");

        logger.LogInformation(
            "Order created successfully: OrderId={OrderId}, CustomerId={CustomerId}, TotalAmount={TotalAmount}, Status={OrderStatus}",
            order.Id, request.CustomerId, order.TotalAmount, order.Status);

        return mapper.Map<OrderDto>(result);
    }
}
