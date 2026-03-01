using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Enums;
using Xunit;

namespace SADC.Order.Management.Tests.Integration;

[Collection("Integration")]
public class OrdersApiTests
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OrdersApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper: creates a customer and returns its ID.
    /// </summary>
    private async Task<Guid> CreateTestCustomerAsync()
    {
        var request = new CreateCustomerRequest("Test Customer", $"test-{Guid.NewGuid():N}@example.com", "ZA");
        var response = await _client.PostAsJsonAsync("/api/customers", request);
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        return customer!.Id;
    }

    /// <summary>
    /// Helper: creates an order for a given customer and returns the OrderDto.
    /// </summary>
    private async Task<OrderDto> CreateTestOrderAsync(Guid customerId)
    {
        var request = new CreateOrderRequest(
            customerId,
            "ZAR",
            new List<CreateOrderLineItemRequest>
            {
                new("SKU-001", 2, 100.00m),
                new("SKU-002", 1, 250.00m)
            });

        var response = await _client.PostAsJsonAsync("/api/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_Returns201WithComputedTotals()
    {
        var customerId = await CreateTestCustomerAsync();

        var order = await CreateTestOrderAsync(customerId);

        order.Should().NotBeNull();
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.CurrencyCode.Should().Be("ZAR");
        order.TotalAmount.Should().Be(450.00m); // 2*100 + 1*250
        order.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_InvalidCurrency_Returns422()
    {
        var customerId = await CreateTestCustomerAsync(); // ZA customer

        var request = new CreateOrderRequest(
            customerId,
            "USD", // not valid for ZA
            new List<CreateOrderLineItemRequest> { new("SKU-001", 1, 10.00m) });

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetOrder_ExistingId_Returns200WithLineItems()
    {
        var customerId = await CreateTestCustomerAsync();
        var created = await CreateTestOrderAsync(customerId);

        var response = await _client.GetAsync($"/api/orders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.Id.Should().Be(created.Id);
        order.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrder_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_Returns200()
    {
        var customerId = await CreateTestCustomerAsync();
        var order = await CreateTestOrderAsync(customerId);

        var request = new UpdateOrderStatusRequest(OrderStatus.Paid);
        var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        updated!.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns422()
    {
        var customerId = await CreateTestCustomerAsync();
        var order = await CreateTestOrderAsync(customerId);

        // Pending → Fulfilled is invalid (must go Pending → Paid → Fulfilled)
        var request = new UpdateOrderStatusRequest(OrderStatus.Fulfilled);
        var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateStatus_FullLifecycle_PendingToPaidToFulfilled()
    {
        var customerId = await CreateTestCustomerAsync();
        var order = await CreateTestOrderAsync(customerId);

        // Pending → Paid
        var payResponse = await _client.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Paid), JsonOptions);
        payResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Paid → Fulfilled
        var fulfillResponse = await _client.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Fulfilled), JsonOptions);
        fulfillResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fulfilled = await fulfillResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        fulfilled!.Status.Should().Be(OrderStatus.Fulfilled);
    }

    [Fact]
    public async Task UpdateStatus_IdempotencyKey_ReturnsSameResponse()
    {
        var customerId = await CreateTestCustomerAsync();
        var order = await CreateTestOrderAsync(customerId);
        var idempotencyKey = Guid.NewGuid().ToString();

        // First request
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/orders/{order.Id}/status")
        {
            Content = JsonContent.Create(new UpdateOrderStatusRequest(OrderStatus.Paid), options: JsonOptions)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await _client.SendAsync(request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await first.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        // Second request with same key — should return cached response
        var retry = new HttpRequestMessage(HttpMethod.Put, $"/api/orders/{order.Id}/status")
        {
            Content = JsonContent.Create(new UpdateOrderStatusRequest(OrderStatus.Paid), options: JsonOptions)
        };
        retry.Headers.Add("Idempotency-Key", idempotencyKey);
        var second = await _client.SendAsync(retry);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await second.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        secondResult!.Id.Should().Be(firstResult!.Id);
        secondResult.Status.Should().Be(firstResult.Status);
    }

    [Fact]
    public async Task ListOrders_FilterByCustomer_ReturnsFiltered()
    {
        var customerId = await CreateTestCustomerAsync();
        await CreateTestOrderAsync(customerId);
        await CreateTestOrderAsync(customerId);

        var response = await _client.GetAsync($"/api/orders?customerId={customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("items");
    }
}
