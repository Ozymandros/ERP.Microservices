using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Sales.API.Controllers;

/// <summary>Provides API endpoints for managing Sales customers, including CRUD operations and export.</summary>
[ApiController]
[Authorize]
[Route("api/sales/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CustomersController> _logger;

    /// <summary>
    /// Initializes a new instance of the CustomersController class.
    /// </summary>
    /// <param name="customerService">The customer Service.</param>
    /// <param name="cacheService">The cache Service.</param>
    /// <param name="logger">The logger.</param>
    public CustomersController(ICustomerService customerService, ICacheService cacheService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Export all customers as XLSX
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// </summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Sales", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx(CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _cacheService.GetStateAsync<IEnumerable<CustomerDto>>("all_customers")
                        ?? await _customerService.ListCustomersAsync(cancellationToken);
            var bytes = customers.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customers.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customers to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting customers" });
        }
    }

    /// <summary>
    /// Export all customers as PDF
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// </summary>
    [HttpGet("export-pdf")]
    [HasPermission("Sales", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf(CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _cacheService.GetStateAsync<IEnumerable<CustomerDto>>("all_customers")
                ?? await _customerService.ListCustomersAsync(cancellationToken);
            var bytes = customers.ExportToPdf();
            return File(bytes, "application/pdf", "Customers.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customers to PDF");
            return StatusCode(500, new { message = "An error occurred exporting customers" });
        }
    }

    /// <summary>
    /// Get all customers (optionally paginated and filtered)
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    [HttpGet]
    [HasPermission("Sales", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
    [ProducesResponseType(typeof(PaginatedResult<CustomerDto>), 200)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        try
        {
            // If query parameters are provided, perform a search/paginated query
            if (Request.Query.Any())
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var spec = new CustomerQuerySpec(query);
                var result = await _customerService.QueryCustomersAsync(spec, cancellationToken);
                return Ok(result);
            }

            var customers = await _cacheService.GetStateAsync<IEnumerable<CustomerDto>>("all_customers");
            if (customers != null)
            {
                return Ok(customers);
            }

            customers = await _customerService.ListCustomersAsync();
            await _cacheService.SaveStateAsync("all_customers", customers);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all customers");
            return StatusCode(500, new { message = "An error occurred retrieving customers" });
        }
    }

    /// <summary>
    /// Get a specific customer by ID - Requires Sales.Read permission
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    [HttpGet("{id}")]
    [HasPermission("Sales", "Read")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKey = "Customer-" + id;
            var customer = await _cacheService.GetStateAsync<CustomerDto>(cacheKey);

            if (customer != null)
            {
                return Ok(customer);
            }

            customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            await _cacheService.SaveStateAsync(cacheKey, customer);
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {@Customer}", new { CustomerId = id });
            var customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
            return customer == null ? NotFound(new { message = $"Customer with ID {id} not found." }) : Ok(customer);
        }
    }

    /// <summary>
    /// Get a specific customer by Name - Requires Sales.Read permission
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    [HttpGet("name/{name}")]
    [HasPermission("Sales", "Read")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByName(string name, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKey = "Customer-Name-" + name;
            var customer = await _cacheService.GetStateAsync<CustomerDto>(cacheKey);

            if (customer != null)
            {
                return Ok(customer);
            }

            customer = await _customerService.GetCustomerByNameAsync(name, cancellationToken);
            if (customer == null)
                return NotFound(new { message = $"Customer with name '{name}' not found." });

            await _cacheService.SaveStateAsync(cacheKey, customer);
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {@Name}", new { Name = name });
            var customer = await _customerService.GetCustomerByNameAsync(name, cancellationToken);
            return customer == null ? NotFound(new { message = $"Customer with name '{name}' not found." }) : Ok(customer);
        }
    }

    /// <summary>
    /// Get a specific customer by Email - Requires Sales.Read permission
    /// </summary>
    /// <param name="email">The email.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    [HttpGet("email/{email}")]
    [HasPermission("Sales", "Read")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByEmail(string email, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKey = "Customer-Email-" + email;
            var customer = await _cacheService.GetStateAsync<CustomerDto>(cacheKey);

            if (customer != null)
            {
                return Ok(customer);
            }

            customer = await _customerService.GetCustomerByEmailAsync(email, cancellationToken);
            if (customer == null)
                return NotFound(new { message = $"Customer with email '{email}' not found." });

            await _cacheService.SaveStateAsync(cacheKey, customer);
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving customer {@Email}",
                new { Email = new MyApp.Shared.Domain.Security.LogSanitizer().Sanitize(email) });
            var customer = await _customerService.GetCustomerByEmailAsync(email, cancellationToken);
            return customer == null ? NotFound(new { message = $"Customer with email '{email}' not found." }) : Ok(customer);
        }
    }

    /// <summary>
    /// Search customers with advanced filtering, sorting, and pagination - Requires Sales.Read permission
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Supported filters: name, email, country, city, isActive
    /// Supported sort fields: id, name, email, city, country, createdAt
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Sales", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        try
        {
            query.BindFiltersFromQuery(Request.Query);
            query.Validate();
            var spec = new CustomerQuerySpec(query);
            var result = await _customerService.QueryCustomersAsync(spec, cancellationToken);
            _logger.LogInformation("Searched customers");
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return StatusCode(500, new { message = "An error occurred searching customers" });
        }
    }

    /// <summary>
    /// Create a new customer - Requires Sales.Create permission
    /// </summary>
    /// <param name="dto">The dto.</param>
    [HttpPost]
    [HasPermission("Sales", "Create")]
    [ProducesResponseType(typeof(CustomerDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var customer = await _customerService.CreateCustomerAsync(dto);
            await _cacheService.RemoveStateAsync("all_customers");
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw;
        }
    }

    /// <summary>
    /// Update an existing customer - Requires Sales.Update permission
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    [HttpPut("{id}")]
    [HasPermission("Sales", "Update")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var customer = await _customerService.UpdateCustomerAsync(id, dto);
            string cacheKey = "Customer-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_customers");
            return Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error updating customer {@Customer}: {@Error}", new { CustomerId = id }, new { Message = ex.Message });
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a customer - Requires Sales.Delete permission
    /// </summary>
    /// <param name="id">The id.</param>
    [HttpDelete("{id}")]
    [HasPermission("Sales", "Delete")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _customerService.DeleteCustomerAsync(id);
            string cacheKey = "Customer-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_customers");
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error deleting customer {@Customer}: {@Error}", new { CustomerId = id }, new { Message = ex.Message });
            return NotFound(new { message = ex.Message });
        }
    }
}
