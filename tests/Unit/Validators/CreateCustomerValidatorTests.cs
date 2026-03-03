using FluentAssertions;
using FluentValidation.TestHelper;
using SADC.Order.Management.Application.Customers.Commands;
using SADC.Order.Management.Application.Customers.Validators;
using Xunit;

namespace SADC.Order.Management.Tests.Unit.Validators;

public class CreateCustomerValidatorTests
{
    private readonly CreateCustomerValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var command = new CreateCustomerCommand("John Doe", "john@example.com", "ZA");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var command = new CreateCustomerCommand("", "john@example.com", "ZA");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_InvalidEmail_HasError()
    {
        var command = new CreateCustomerCommand("John", "not-an-email", "ZA");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_NonSadcCountry_HasError()
    {
        var command = new CreateCustomerCommand("John", "john@example.com", "US");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Theory]
    [InlineData("ZA")]
    [InlineData("NA")]
    [InlineData("BW")]
    [InlineData("MZ")]
    public void Validate_ValidSadcCountries_NoCountryError(string country)
    {
        var command = new CreateCustomerCommand("John", "john@example.com", country);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CountryCode);
    }
}
