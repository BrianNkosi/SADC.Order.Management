using FluentAssertions;
using FluentValidation.TestHelper;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Application.Customers.Validators;
using Xunit;

namespace SADC.Order.Management.Tests.Unit.Validators;

public class CreateCustomerValidatorTests
{
    private readonly CreateCustomerValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var request = new CreateCustomerRequest("John Doe", "john@example.com", "ZA");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var request = new CreateCustomerRequest("", "john@example.com", "ZA");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_InvalidEmail_HasError()
    {
        var request = new CreateCustomerRequest("John", "not-an-email", "ZA");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_NonSadcCountry_HasError()
    {
        var request = new CreateCustomerRequest("John", "john@example.com", "US");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Theory]
    [InlineData("ZA")]
    [InlineData("NA")]
    [InlineData("BW")]
    [InlineData("MZ")]
    public void Validate_ValidSadcCountries_NoCountryError(string country)
    {
        var request = new CreateCustomerRequest("John", "john@example.com", country);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CountryCode);
    }
}
