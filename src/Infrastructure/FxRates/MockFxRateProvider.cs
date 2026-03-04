using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Infrastructure.Diagnostics;

namespace SADC.Order.Management.Infrastructure.FxRates;

/// <summary>
/// Mocked FX rate provider with in-memory caching.
/// In production, this would call an external API (e.g., Open Exchange Rates).
/// Uses banker's rounding (MidpointRounding.ToEven) per SADC reporting requirements.
/// </summary>
public class MockFxRateProvider : IFxRateProvider
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MockFxRateProvider> _logger;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Mocked rates relative to ZAR (1 ZAR = X units of currency).
    /// In real implementation, these would come from an API.
    /// </summary>
    private static readonly Dictionary<string, decimal> RatesToZar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ZAR"] = 1.0m,
        ["NAD"] = 1.0m,       // NAD pegged 1:1 to ZAR
        ["LSL"] = 1.0m,       // LSL pegged 1:1 to ZAR
        ["SZL"] = 1.0m,       // SZL pegged 1:1 to ZAR
        ["BWP"] = 0.72m,      // 1 ZAR ≈ 0.72 BWP → 1 BWP ≈ 1.39 ZAR
        ["MZN"] = 3.50m,      // 1 ZAR ≈ 3.50 MZN
        ["ZMW"] = 1.45m,      // 1 ZAR ≈ 1.45 ZMW
        ["ZWL"] = 18.50m,     // 1 ZAR ≈ 18.50 ZWL (volatile)
        ["USD"] = 0.054m,     // 1 ZAR ≈ 0.054 USD → 1 USD ≈ 18.52 ZAR
        ["AOA"] = 49.50m,     // 1 ZAR ≈ 49.50 AOA
        ["CDF"] = 152.0m,     // 1 ZAR ≈ 152 CDF
        ["MGA"] = 248.0m,     // 1 ZAR ≈ 248 MGA
        ["MWK"] = 95.0m,      // 1 ZAR ≈ 95 MWK
        ["MUR"] = 2.50m,      // 1 ZAR ≈ 2.50 MUR
        ["SCR"] = 0.78m,      // 1 ZAR ≈ 0.78 SCR
        ["TZS"] = 144.0m,     // 1 ZAR ≈ 144 TZS
        ["KMF"] = 25.0m       // 1 ZAR ≈ 25 KMF
    };

    public MockFxRateProvider(IMemoryCache cache, ILogger<MockFxRateProvider> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"fx:{fromCurrency.ToUpperInvariant()}:{toCurrency.ToUpperInvariant()}";

        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            TelemetryConstants.FxCacheHits.Add(1, new KeyValuePair<string, object?>("currency_pair", cacheKey));
            _logger.LogDebug("FX cache hit: {CacheKey} = {Rate}", cacheKey, cachedRate);
            return Task.FromResult(cachedRate);
        }

        TelemetryConstants.FxCacheMisses.Add(1, new KeyValuePair<string, object?>("currency_pair", cacheKey));
        _logger.LogDebug("FX cache miss: {CacheKey}", cacheKey);

        var from = fromCurrency.ToUpperInvariant();
        var to = toCurrency.ToUpperInvariant();

        if (!RatesToZar.TryGetValue(from, out var fromRate))
            throw new ArgumentException($"Unsupported currency: {from}");

        if (!RatesToZar.TryGetValue(to, out var toRate))
            throw new ArgumentException($"Unsupported currency: {to}");

        var rate = toRate / fromRate;

        _cache.Set(cacheKey, rate, _cacheTtl);

        return Task.FromResult(rate);
    }

    /// <summary>
    /// Converts an amount from one currency to ZAR using banker's rounding.
    /// </summary>
    public async Task<decimal> ConvertToZarAsync(decimal amount, string fromCurrency, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rate = await GetRateAsync(fromCurrency, "ZAR", cancellationToken);
        var result = Math.Round(amount * rate, 2, MidpointRounding.ToEven);
        sw.Stop();
        TelemetryConstants.FxConversionDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("from_currency", fromCurrency.ToUpperInvariant()));
        return result;
    }
}
