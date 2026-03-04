using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Reports.DTOs;

namespace SADC.Order.Management.Application.Reports.Queries;

/// <summary>
/// Handles GetOrderTotalsInZarQuery. Computes order totals in ZAR using FX rates.
/// </summary>
public sealed class GetOrderTotalsInZarQueryHandler(
    IOrderManagementDbContext context,
    IFxRateProvider fxRateProvider,
    ILogger<GetOrderTotalsInZarQueryHandler> logger)
    : IRequestHandler<GetOrderTotalsInZarQuery, OrderTotalsInZarDto>
{
    public async Task<OrderTotalsInZarDto> Handle(GetOrderTotalsInZarQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating order totals in ZAR report");

        var ordersByCurrency = await context.Orders
            .AsNoTracking()
            .GroupBy(o => o.CurrencyCode)
            .Select(g => new
            {
                CurrencyCode = g.Key,
                TotalAmount = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {CurrencyGroupCount} currency group(s) to convert", ordersByCurrency.Count);

        var summaryTasks = ordersByCurrency.Select(async group =>
        {
            var rate = await fxRateProvider.GetRateAsync(group.CurrencyCode, "ZAR", cancellationToken);
            var amountInZar = Math.Round(group.TotalAmount * rate, 2, MidpointRounding.ToEven);

            logger.LogDebug(
                "FX conversion: {CurrencyCode} -> ZAR, Rate={ExchangeRate}, OriginalTotal={OriginalTotal}, ConvertedTotal={ConvertedTotal}, OrderCount={OrderCount}",
                group.CurrencyCode, rate, group.TotalAmount, amountInZar, group.OrderCount);

            return new CurrencySummaryDto(group.CurrencyCode, group.TotalAmount, rate, amountInZar, group.OrderCount);
        });

        var currencySummaries = await Task.WhenAll(summaryTasks);

        var grandTotalZar = currencySummaries.Sum(s => s.TotalInZar);
        var totalOrders = currencySummaries.Sum(s => s.OrderCount);

        logger.LogInformation(
            "ZAR report complete: GrandTotalZar={GrandTotalZar}, TotalOrders={TotalOrders}",
            grandTotalZar, totalOrders);

        return new OrderTotalsInZarDto(
            grandTotalZar,
            totalOrders,
            "Banker's rounding (MidpointRounding.ToEven)",
            [.. currencySummaries]);
    }
}
