using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Infrastructure.Persistence;
using Xunit;

namespace SADC.Order.Management.Tests.Integration;

public class CustomersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CustomersApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with in-memory for tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrderManagementDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<OrderManagementDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateCustomer_ValidRequest_Returns201()
    {
        var request = new CreateCustomerRequest("Test Customer", "test@example.com", "ZA");

        var response = await _client.PostAsJsonAsync("/api/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.Name.Should().Be("Test Customer");
        customer.CountryCode.Should().Be("ZA");
    }

    [Fact]
    public async Task CreateCustomer_InvalidCountry_Returns400()
    {
        var request = new CreateCustomerRequest("Test", "test@example.com", "US");

        var response = await _client.PostAsJsonAsync("/api/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomer_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
