namespace SADC.Order.Management.Application.Common.Interfaces;

/// <summary>
/// Provides foreign exchange rates for currency conversion.
/// </summary>
public interface IFxRateProvider
{
    /// <summary>
    /// Gets the exchange rate to convert from <paramref name="fromCurrency"/> to <paramref name="toCurrency"/>.
    /// </summary>
    /// <returns>The multiplier to convert an amount from source to target currency.</returns>
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}
