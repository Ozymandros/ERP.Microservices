using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Sales.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/sales/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, ICacheService cacheService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Get all customers - Requires Sales.Read permission
        /// </summary>
        [HttpGet]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
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
                var customers = await _customerService.ListCustomersAsync();
                return Ok(customers);
            }
        }

        /// <summary>
        /// Get a specific customer by ID - Requires Sales.Read permission
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                string cacheKey = $"Customer-{id}";
                var customer = await _cacheService.GetStateAsync<CustomerDto>(cacheKey);

                if (customer != null)
                {
                    return Ok(customer);
                }

                customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                    return NotFound(new { message = $"Customer with ID {id} not found." });

                await _cacheService.SaveStateAsync(cacheKey, customer);
                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
                var customer = await _customerService.GetCustomerByIdAsync(id);
                return customer == null ? NotFound(new { message = $"Customer with ID {id} not found." }) : Ok(customer);
            }
        }

        /// <summary>
        /// Search customers with advanced filtering, sorting, and pagination - Requires Sales.Read permission
        /// </summary>
        /// <remarks>
        /// Supported filters: name, email, country, city, isActive
        /// Supported sort fields: id, name, email, city, country, createdAt
        /// </remarks>
        [HttpGet("search")]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(PaginatedResult<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] QuerySpec query)
        {
            try
            {
                query.Validate();
                var spec = new CustomerQuerySpec(query);
                var result = await _customerService.QueryCustomersAsync(spec);
                _logger.LogInformation("Searched customers with query: Page {Page}, PageSize {PageSize}, SortBy {SortBy}", query.Page, query.PageSize, query.SortBy);
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
        [HttpPut("{id}")]
        [HasPermission("Sales", "Update")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var customer = await _customerService.UpdateCustomerAsync(id, dto);
                string cacheKey = $"Customer-{id}";
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_customers");
                return Ok(customer);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error updating customer {CustomerId}: {Message}", id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a customer - Requires Sales.Delete permission
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission("Sales", "Delete")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                string cacheKey = $"Customer-{id}";
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_customers");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error deleting customer {CustomerId}: {Message}", id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
