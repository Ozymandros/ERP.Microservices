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
    [Authorize()]
    [Route("api/sales/orders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(ISalesOrderService salesOrderService, ICacheService cacheService, ILogger<SalesOrdersController> logger)
        {
            _salesOrderService = salesOrderService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Get all sales orders - Requires Sales.Read permission
        /// </summary>
        [HttpGet]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(IEnumerable<SalesOrderDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var orders = await _cacheService.GetStateAsync<IEnumerable<SalesOrderDto>>("all_sales_orders");
                if (orders != null)
                {
                    _logger.LogInformation("Retrieved all sales orders from cache");
                    return Ok(orders);
                }

                orders = await _salesOrderService.ListSalesOrdersAsync();
                await _cacheService.SaveStateAsync("all_sales_orders", orders);
                _logger.LogInformation("Retrieved all sales orders from database and cached");
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sales orders");
                var orders = await _salesOrderService.ListSalesOrdersAsync();
                return Ok(orders);
            }
        }

        /// <summary>
        /// Get a specific sales order by ID - Requires Sales.Read permission
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(SalesOrderDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                string cacheKey = "SalesOrder-" + id;
                var order = await _cacheService.GetStateAsync<SalesOrderDto>(cacheKey);

                if (order != null)
                {
                    _logger.LogInformation("Retrieved sales order {@Order} from cache", new { OrderId = id });
                    return Ok(order);
                }

                order = await _salesOrderService.GetSalesOrderByIdAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Sales order {@Order} not found", new { OrderId = id });
                    return NotFound(new { message = $"Order with ID {id} not found." });
                }

                await _cacheService.SaveStateAsync(cacheKey, order);
                _logger.LogInformation("Retrieved sales order {@Order} from database and cached", new { OrderId = id });
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales order {@Order}", new { OrderId = id });
                var order = await _salesOrderService.GetSalesOrderByIdAsync(id);
                return order == null ? NotFound(new { message = $"Order with ID {id} not found." }) : Ok(order);
            }
        }

        /// <summary>
        /// Search sales orders with advanced filtering, sorting, and pagination - Requires Sales.Read permission
        /// </summary>
        /// <remarks>
        /// Supported filters: orderNumber, customerId, status, minTotal, maxTotal
        /// Supported sort fields: id, orderNumber, status, totalAmount, createdAt, orderDate
        /// </remarks>
        [HttpGet("search")]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(PaginatedResult<SalesOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] QuerySpec query)
        {
            try
            {
                query.Validate();
                var spec = new SalesOrderQuerySpec(query);
                var result = await _salesOrderService.QuerySalesOrdersAsync(spec);
                _logger.LogInformation("Searched sales orders with query: {@Query}", query);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid query specification");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching sales orders");
                return StatusCode(500, new { message = "An error occurred searching sales orders" });
            }
        }

        /// <summary>
        /// Create a new sales order - Requires Sales.Create permission
        /// </summary>
        [HttpPost]
        [HasPermission("Sales", "Create")]
        [ProducesResponseType(typeof(SalesOrderDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var order = await _salesOrderService.CreateSalesOrderAsync(dto);
                await _cacheService.RemoveStateAsync("all_sales_orders");
                _logger.LogInformation("Sales order {@Order} created and cache invalidated", new { OrderId = order.Id });
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error creating sales order: {@Error}", new { Message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing sales order - Requires Sales.Update permission
        /// </summary>
        [HttpPut("{id}")]
        [HasPermission("Sales", "Update")]
        [ProducesResponseType(typeof(SalesOrderDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var order = await _salesOrderService.UpdateSalesOrderAsync(id, dto);
                string cacheKey = "SalesOrder-" + id;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_sales_orders");
                _logger.LogInformation("Sales order {@Order} updated and cache invalidated", new { OrderId = id });
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error updating sales order {@Order}: {@Error}", new { OrderId = id }, new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a sales order - Requires Sales.Delete permission
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission("Sales", "Delete")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _salesOrderService.DeleteSalesOrderAsync(id);
                string cacheKey = "SalesOrder-" + id;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_sales_orders");
                _logger.LogInformation("Sales order {@Order} deleted and cache invalidated", new { OrderId = id });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error deleting sales order {@Order}: {@Error}", new { OrderId = id }, new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
