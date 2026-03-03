using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SADC.Order.Management.Application.Customers.Commands;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Application.Customers.Queries;

namespace SADC.Order.Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">Customer creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created customer.</returns>
    /// <response code="201">Customer created successfully.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(request.Name, request.Email, request.CountryCode);
        var customer = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    /// <param name="id">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer.</returns>
    /// <response code="200">Customer found.</response>
    /// <response code="404">Customer not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        if (customer is null)
            return NotFound();

        return Ok(customer);
    }

    /// <summary>
    /// Searches customers with pagination.
    /// </summary>
    /// <param name="search">Optional search term (name or email).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (max: 100, default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Paginated list of customers.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchCustomersQuery(search, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
