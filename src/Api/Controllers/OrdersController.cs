using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SADC.Order.Management.Application.Orders;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Enums;

namespace SADC.Order.Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Creates a new order with line items.
    /// </summary>
    /// <param name="request">Order creation details including line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created order with computed totals.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="422">Business rule violation (invalid country/currency).</response>
    [HttpPost]
    [Authorize(Policy = "Orders.Write")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Gets an order by ID, including line items.
    /// </summary>
    /// <param name="id">Order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order with line items.</returns>
    /// <response code="200">Order found.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Orders.Read")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        // ETag for caching
        var etag = $"\"{order.Id}-{order.UpdatedAtUtc?.Ticks ?? order.CreatedAtUtc.Ticks}\"";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(order);
    }

    /// <summary>
    /// Lists orders with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="customerId">Filter by customer ID.</param>
    /// <param name="status">Filter by order status.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (max: 100, default: 20).</param>
    /// <param name="sortBy">Sort field (created, total, status). Default: created.</param>
    /// <param name="descending">Sort descending (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Paginated list of orders.</response>
    [HttpGet]
    [Authorize(Policy = "Orders.Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.ListAsync(
            customerId, status, page, pageSize, sortBy, descending, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the status of an order with validated transitions.
    /// Supports idempotent requests via Idempotency-Key header.
    /// </summary>
    /// <param name="id">Order ID.</param>
    /// <param name="request">New status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    /// <response code="200">Status updated successfully.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="409">Concurrency conflict.</response>
    /// <response code="422">Invalid state transition.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "Orders.Write")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        // Support idempotency key from header
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var effectiveRequest = request with { IdempotencyKey = idempotencyKey ?? request.IdempotencyKey };

        var order = await _orderService.UpdateStatusAsync(id, effectiveRequest, cancellationToken);
        return Ok(order);
    }
}
