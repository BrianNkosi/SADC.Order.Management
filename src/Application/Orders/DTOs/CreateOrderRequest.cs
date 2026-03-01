namespace SADC.Order.Management.Application.Orders.DTOs;

public record CreateOrderRequest(
    Guid CustomerId,
    string CurrencyCode,
    List<CreateOrderLineItemRequest> LineItems);

public record CreateOrderLineItemRequest(
    string ProductSku,
    int Quantity,
    decimal UnitPrice);
