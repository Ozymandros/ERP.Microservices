using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;

namespace MyApp.Sales.API.Controllers
{
    [ApiController]
    [Route("api/sales/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _customerService.ListCustomersAsync();
            return Ok(customers);
        }

        /// <summary>
        /// Get a specific customer by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });
            return Ok(customer);
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _customerService.CreateCustomerAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        /// <summary>
        /// Update an existing customer
        /// </summary>
        [HttpPut("{id}")]
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
                return Ok(customer);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a customer
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
