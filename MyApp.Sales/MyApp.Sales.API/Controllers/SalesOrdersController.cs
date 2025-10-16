using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;

namespace MyApp.Sales.API.Controllers
{
    [ApiController]
    [Route("api/sales/orders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrdersController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        /// <summary>
        /// Get all sales orders
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SalesOrderDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _salesOrderService.ListSalesOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Get a specific sales order by ID
        /// </summary>
        [HttpGet("{id}")]
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
        /// Create a new sales order
        /// </summary>
        [HttpPost]
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
        /// Update an existing sales order
        /// </summary>
        [HttpPut("{id}")]
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
        /// Delete a sales order
        /// </summary>
        [HttpDelete("{id}")]
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
