using MediatR;
using Microsoft.EntityFrameworkCore;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Application.Reports.DTOs;

namespace SADC.Order.Management.Application.Reports.Queries;

/// <summary>
/// Handles GetOrderTotalsInZarQuery. Computes order totals in ZAR using FX rates.
/// </summary>
public sealed class GetOrderTotalsInZarQueryHandler(
    IOrderManagementDbContext context,
    IFxRateProvider fxRateProvider)
    : IRequestHandler<GetOrderTotalsInZarQuery, OrderTotalsInZarDto>
{
    public async Task<OrderTotalsInZarDto> Handle(GetOrderTotalsInZarQuery request, CancellationToken cancellationToken)
    {
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

        var currencySummaries = new List<CurrencySummaryDto>();
        decimal grandTotalZar = 0;
        int totalOrders = 0;

        foreach (var group in ordersByCurrency)
        {
            var rate = await fxRateProvider.GetRateAsync(group.CurrencyCode, "ZAR", cancellationToken);
            var amountInZar = Math.Round(group.TotalAmount * rate, 2, MidpointRounding.ToEven);

            currencySummaries.Add(new CurrencySummaryDto(
                group.CurrencyCode,
                group.TotalAmount,
                rate,
                amountInZar,
                group.OrderCount));

            grandTotalZar += amountInZar;
            totalOrders += group.OrderCount;
        }

        return new OrderTotalsInZarDto(
            grandTotalZar,
            totalOrders,
            "Banker's rounding (MidpointRounding.ToEven)",
            currencySummaries);
    }
}
