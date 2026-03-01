using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Application.Orders.DTOs;

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    string CurrencyCode,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<OrderLineItemDto> LineItems);

public record OrderLineItemDto(
    Guid Id,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    string CurrencyCode,
    decimal TotalAmount,
    int LineItemCount,
    DateTime CreatedAtUtc);
