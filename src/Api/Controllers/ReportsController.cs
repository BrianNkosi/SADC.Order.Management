using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Infrastructure.Persistence;

namespace SADC.Order.Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "Orders.Read")]
public class ReportsController : ControllerBase
{
    private readonly IOrderManagementDbContext _context;
    private readonly IFxRateProvider _fxRateProvider;

    public ReportsController(IOrderManagementDbContext context, IFxRateProvider fxRateProvider)
    {
        _context = context;
        _fxRateProvider = fxRateProvider;
    }

    /// <summary>
    /// Returns order totals converted to ZAR with per-currency breakdown.
    /// Uses banker's rounding (MidpointRounding.ToEven).
    /// </summary>
    /// <response code="200">ZAR conversion report.</response>
    [HttpGet("orders/zar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderTotalsInZar(CancellationToken cancellationToken)
    {
        var ordersByCurrency = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.CurrencyCode)
            .Select(g => new
            {
                CurrencyCode = g.Key,
                TotalAmount = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var currencySummaries = new List<object>();
        decimal grandTotalZar = 0;
        int totalOrders = 0;

        foreach (var group in ordersByCurrency)
        {
            var rate = await _fxRateProvider.GetRateAsync(group.CurrencyCode, "ZAR", cancellationToken);
            var amountInZar = Math.Round(group.TotalAmount * rate, 2, MidpointRounding.ToEven);

            currencySummaries.Add(new
            {
                group.CurrencyCode,
                OriginalTotal = group.TotalAmount,
                ExchangeRate = rate,
                TotalInZar = amountInZar,
                group.OrderCount
            });

            grandTotalZar += amountInZar;
            totalOrders += group.OrderCount;
        }

        return Ok(new
        {
            GrandTotalZar = grandTotalZar,
            TotalOrders = totalOrders,
            RoundingStrategy = "Banker's rounding (MidpointRounding.ToEven)",
            CurrencyBreakdown = currencySummaries
        });
    }
}
