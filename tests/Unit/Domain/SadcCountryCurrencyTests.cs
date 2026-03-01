using FluentAssertions;
using SADC.Order.Management.Domain.ValueObjects;
using Xunit;

namespace SADC.Order.Management.Tests.Unit.Domain;

public class SadcCountryCurrencyTests
{
    [Theory]
    [InlineData("ZA")]
    [InlineData("NA")]
    [InlineData("BW")]
    [InlineData("MZ")]
    public void IsValidCountry_ValidSadcCountry_ReturnsTrue(string code)
    {
        SadcCountryCurrency.IsValidCountry(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("US")]
    [InlineData("GB")]
    [InlineData("XX")]
    public void IsValidCountry_NonSadcCountry_ReturnsFalse(string code)
    {
        SadcCountryCurrency.IsValidCountry(code).Should().BeFalse();
    }

    [Theory]
    [InlineData("ZA", "ZAR", true)]
    [InlineData("NA", "NAD", true)]
    [InlineData("NA", "ZAR", true)]     // CMA rule: NAD accepts ZAR
    [InlineData("LS", "ZAR", true)]     // CMA rule: LSL accepts ZAR
    [InlineData("SZ", "ZAR", true)]     // CMA rule: SZL accepts ZAR
    [InlineData("ZA", "NAD", false)]    // ZA only accepts ZAR
    [InlineData("BW", "ZAR", false)]    // Non-CMA: BWP only
    [InlineData("ZW", "USD", true)]     // Zimbabwe accepts USD
    [InlineData("ZW", "ZWL", true)]     // Zimbabwe local currency
    public void IsValidCurrencyForCountry_ReturnsExpectedResult(string country, string currency, bool expected)
    {
        SadcCountryCurrency.IsValidCurrencyForCountry(country, currency).Should().Be(expected);
    }

    [Theory]
    [InlineData("ZA")]
    [InlineData("NA")]
    [InlineData("LS")]
    [InlineData("SZ")]
    public void IsCmaMember_CmaCountries_ReturnsTrue(string code)
    {
        SadcCountryCurrency.IsCmaMember(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("BW")]
    [InlineData("MZ")]
    public void IsCmaMember_NonCmaCountries_ReturnsFalse(string code)
    {
        SadcCountryCurrency.IsCmaMember(code).Should().BeFalse();
    }

    [Fact]
    public void GetValidCurrencies_ForNamibia_ReturnsNadAndZar()
    {
        var currencies = SadcCountryCurrency.GetValidCurrencies("NA");
        currencies.Should().BeEquivalentTo(["NAD", "ZAR"]);
    }

    [Fact]
    public void GetValidCurrencies_ForInvalidCountry_ReturnsEmpty()
    {
        SadcCountryCurrency.GetValidCurrencies("XX").Should().BeEmpty();
    }
}
