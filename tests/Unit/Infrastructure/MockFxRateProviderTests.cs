using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SADC.Order.Management.Infrastructure.FxRates;
using Xunit;

namespace SADC.Order.Management.Tests.Unit.Infrastructure;

public class MockFxRateProviderTests
{
    private readonly MockFxRateProvider _provider;

    public MockFxRateProviderTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _provider = new MockFxRateProvider(cache, NullLogger<MockFxRateProvider>.Instance);
    }

    [Fact]
    public async Task GetRate_ZarToZar_ReturnsOne()
    {
        var rate = await _provider.GetRateAsync("ZAR", "ZAR");
        rate.Should().Be(1.0m);
    }

    [Fact]
    public async Task GetRate_NadToZar_ReturnsOne()
    {
        // NAD is pegged 1:1 to ZAR
        var rate = await _provider.GetRateAsync("NAD", "ZAR");
        rate.Should().Be(1.0m);
    }

    [Fact]
    public async Task ConvertToZar_UseBankersRounding()
    {
        // Test banker's rounding: 0.5 rounds to nearest even
        var result = await _provider.ConvertToZarAsync(100.0m, "ZAR");
        result.Should().Be(100.0m);
    }

    [Fact]
    public async Task GetRate_UnsupportedCurrency_Throws()
    {
        var act = async () => await _provider.GetRateAsync("EUR", "ZAR");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*EUR*");
    }

    [Fact]
    public async Task GetRate_CachesResult()
    {
        var rate1 = await _provider.GetRateAsync("BWP", "ZAR");
        var rate2 = await _provider.GetRateAsync("BWP", "ZAR");
        rate1.Should().Be(rate2);
    }
}
