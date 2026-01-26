using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Sales.API.Controllers
{
    [ApiController]
    [Authorize]
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
        /// Export all sales orders as XLSX
        /// </summary>
        [HttpGet("export-xlsx")]
        [HasPermission("Sales", "Read")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToXlsx()
        {
            try
            {
                var orders = await _cacheService.GetStateAsync<IEnumerable<SalesOrderDto>>("all_sales_orders")
                            ?? await _salesOrderService.ListSalesOrdersAsync();
                var bytes = orders.ExportToXlsx();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesOrders.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales orders to XLSX");
                return StatusCode(500, new { message = "An error occurred exporting sales orders" });
            }
        }

        /// <summary>
        /// Export all sales orders as PDF
        /// </summary>
        [HttpGet("export-pdf")]
        [HasPermission("Sales", "Read")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToPdf()
        {
            try
            {
                var orders = await _cacheService.GetStateAsync<IEnumerable<SalesOrderDto>>("all_sales_orders")
                    ?? await _salesOrderService.ListSalesOrdersAsync();
                var bytes = orders.ExportToPdf();
                return File(bytes, "application/pdf", "SalesOrders.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales orders to PDF");
                return StatusCode(500, new { message = "An error occurred exporting sales orders" });
            }
        }

        /// <summary>
        /// Get all sales orders (paginated, filterable)
        /// </summary>
        [HttpGet]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(PaginatedResult<SalesOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] QuerySpec query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                query.Validate();
                var spec = new SalesOrderQuerySpec(query);
                var result = await _salesOrderService.QuerySalesOrdersAsync(spec);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid query specification");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales orders");
                return StatusCode(500, new { message = "An error occurred retrieving sales orders" });
            }
        }

        /// <summary>
        /// Search sales orders (advanced query)
        /// </summary>
        [HttpGet("search")]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(PaginatedResult<SalesOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromQuery] QuerySpec query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
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
        /// Get a specific customer by ID - Requires Sales.Read permission
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
                var salesOrder = await _cacheService.GetStateAsync<SalesOrderDto>(cacheKey);

                if (salesOrder != null)
                {
                    return Ok(salesOrder);
                }

                salesOrder = await _salesOrderService.GetSalesOrderByIdAsync(id);
                if (salesOrder == null)
                    return NotFound(new { message = $"SalesOrder with ID {id} not found." });

                await _cacheService.SaveStateAsync(cacheKey, salesOrder);
                return Ok(salesOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving salesOrder {@SalesOrder}", new { SalesOrderId = id });
                var salesOrder = await _salesOrderService.GetSalesOrderByIdAsync(id);
                return salesOrder == null ? NotFound(new { message = $"SalesOrder with ID {id} not found." }) : Ok(salesOrder);
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

        /// <summary>
        /// Create a quote with stock availability validation - Requires Sales.Create permission
        /// </summary>
        [HttpPost("quotes")]
        [HasPermission("Sales", "Create")]
        [ProducesResponseType(typeof(SalesOrderDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteDto dto)
        {
            try
            {
                var result = await _salesOrderService.CreateQuoteAsync(dto);
                await _cacheService.RemoveStateAsync("all_sales_orders");
                _logger.LogInformation(
                    "Quote {@Quote} created and cache invalidated",
                    new { QuoteId = result.Id });
                return CreatedAtAction("Get", new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quote");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Confirm a quote and convert to fulfillment order - Requires Sales.Update permission
        /// </summary>
        [HttpPost("quotes/{id}/confirm")]
        [HasPermission("Sales", "Update")]
        [ProducesResponseType(typeof(SalesOrderDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConfirmQuote(Guid id, [FromBody] ConfirmQuoteDto dto)
        {
            try
            {
                // Ensure the ID matches
                if (id != dto.QuoteId)
                {
                    return BadRequest(new { error = "Quote ID mismatch" });
                }

                var result = await _salesOrderService.ConfirmQuoteAsync(dto);
                string cacheKey = "SalesOrder-" + id;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_sales_orders");
                _logger.LogInformation(
                    "Quote {@Quote} confirmed and cache invalidated",
                    new { QuoteId = id, OrderId = result.ConvertedToOrderId });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error confirming quote {@Quote}: {@Error}", new { QuoteId = id }, new { Message = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming quote {@Quote}", new { QuoteId = id });
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Check stock availability for quote items - Requires Sales.Read permission
        /// </summary>
        [HttpPost("quotes/check-availability")]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(List<StockAvailabilityCheckDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CheckStockAvailability([FromBody] List<CreateUpdateSalesOrderLineDto> lines)
        {
            try
            {
                var result = await _salesOrderService.CheckStockAvailabilityAsync(lines);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stock availability");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
