using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SADC.Order.Management.Application.Reports.Queries;

namespace SADC.Order.Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "Orders.Read")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
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
        var result = await _sender.Send(new GetOrderTotalsInZarQuery(), cancellationToken);
        return Ok(result);
    }
}
