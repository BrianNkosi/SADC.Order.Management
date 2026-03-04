using System.Text.Json;
using AutoMapper;
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
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<CreateOrderCommandHandler> logger)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating order: CustomerId={CustomerId}, Currency={CurrencyCode}, LineItems={LineItemCount}",
            request.CustomerId, request.CurrencyCode, request.LineItems.Count);

        var customer = await FindCustomerAsync(request.CustomerId, cancellationToken);
        var currencyCode = ValidateCurrency(customer, request.CurrencyCode);

        var order = BuildOrder(request, customer, currencyCode);
        var outboxMessage = BuildOutboxMessage(order);

        context.Orders.Add(order);
        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order created: OrderId={OrderId}, TotalAmount={TotalAmount}",
            order.Id, order.TotalAmount);

        return mapper.Map<OrderDto>(order);
    }

    private async Task<Customer> FindCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer is null)
        {
            logger.LogWarning("Customer not found: CustomerId={CustomerId}", customerId);
            throw new KeyNotFoundException($"Customer '{customerId}' not found.");
        }

        return customer;
    }

    private string ValidateCurrency(Customer customer, string rawCurrencyCode)
    {
        var currencyCode = rawCurrencyCode.ToUpperInvariant();

        if (SadcCountryCurrency.IsValidCurrencyForCountry(customer.CountryCode, currencyCode))
            return currencyCode;

        var validCurrencies = SadcCountryCurrency.GetValidCurrencies(customer.CountryCode);

        logger.LogWarning(
            "Invalid currency {CurrencyCode} for country {CountryCode}. Valid: {ValidCurrencies}",
            currencyCode, customer.CountryCode, string.Join(", ", validCurrencies));

        throw new InvalidOperationException(
            $"Currency '{currencyCode}' is not valid for country '{customer.CountryCode}'. " +
            $"Valid currencies: {string.Join(", ", validCurrencies)}.");
    }

    private static OrderEntity BuildOrder(CreateOrderCommand request, Customer customer, string currencyCode)
    {
        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Customer = customer,
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
        return order;
    }

    private OutboxMessage BuildOutboxMessage(OrderEntity order) => new()
    {
        Id = Guid.NewGuid(),
        AggregateType = "Order",
        AggregateId = order.Id,
        Type = "OrderCreated",
        OccurredAtUtc = DateTime.UtcNow,
        CreatedAtUtc = DateTime.UtcNow,
        Version = 1,
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
        })
    };
}
