using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;

namespace MyApp.Sales.API.Controllers
{
    [ApiController]
    [Authorize()]
    [Route("api/sales/orders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrdersController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        /// <summary>
        /// Get all sales orders - Requires Sales.Read permission
        /// </summary>
        [HttpGet]
        [HasPermission("Sales", "Read")]
        [ProducesResponseType(typeof(IEnumerable<SalesOrderDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _salesOrderService.ListSalesOrdersAsync();
            return Ok(orders);
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
            var order = await _salesOrderService.GetSalesOrderByIdAsync(id);
            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found." });
            return Ok(order);
        }

        /// <summary>
        /// Create a new sales order - Requires Sales.Write permission
        /// </summary>
        [HttpPost]
        [HasPermission("Sales", "Write")]
        [ProducesResponseType(typeof(SalesOrderDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var order = await _salesOrderService.CreateSalesOrderAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing sales order - Requires Sales.Write permission
        /// </summary>
        [HttpPut("{id}")]
        [HasPermission("Sales", "Write")]
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
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
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
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
