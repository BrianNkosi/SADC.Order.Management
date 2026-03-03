namespace SADC.Order.Management.Application.Reports.DTOs;

public record CurrencySummaryDto(
    string CurrencyCode,
    decimal OriginalTotal,
    decimal ExchangeRate,
    decimal TotalInZar,
    int OrderCount);

public record OrderTotalsInZarDto(
    decimal GrandTotalZar,
    int TotalOrders,
    string RoundingStrategy,
    IReadOnlyList<CurrencySummaryDto> CurrencyBreakdown);
