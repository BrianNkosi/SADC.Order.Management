using FluentAssertions;
using FluentValidation.TestHelper;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Application.Orders.Validators;
using Xunit;

namespace SADC.Order.Management.Tests.Unit.Validators;

public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            "ZAR",
            [new CreateOrderLineItemRequest("SKU-001", 2, 10.00m)]);

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCustomerId_HasError()
    {
        var request = new CreateOrderRequest(
            Guid.Empty,
            "ZAR",
            [new CreateOrderLineItemRequest("SKU-001", 2, 10.00m)]);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Validate_NoLineItems_HasError()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            "ZAR",
            []);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.LineItems);
    }

    [Fact]
    public void Validate_ZeroQuantity_HasError()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            "ZAR",
            [new CreateOrderLineItemRequest("SKU-001", 0, 10.00m)]);

        var result = _validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_NegativeUnitPrice_HasError()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            "ZAR",
            [new CreateOrderLineItemRequest("SKU-001", 1, -5.00m)]);

        var result = _validator.TestValidate(request);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_InvalidCurrency_HasError()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            "EUR",
            [new CreateOrderLineItemRequest("SKU-001", 1, 10.00m)]);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }
}
