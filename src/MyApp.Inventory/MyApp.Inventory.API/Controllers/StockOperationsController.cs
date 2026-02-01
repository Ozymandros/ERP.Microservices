using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Inventory.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/inventory/stock-operations")]
    public class StockOperationsController : ControllerBase
    {
        private readonly IWarehouseStockService _warehouseStockService;
        private readonly ILogger<StockOperationsController> _logger;

        public StockOperationsController(IWarehouseStockService warehouseStockService, ILogger<StockOperationsController> logger)
        {
            _warehouseStockService = warehouseStockService;
            _logger = logger;
        }

        [HttpPost("reserve")]
        [HasPermission("Inventory", "Update")]
        [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReserveStock([FromBody] ReserveStockDto dto)
        {
            try
            {
                var reservation = await _warehouseStockService.ReserveStockAsync(dto);
                return CreatedAtAction(nameof(ReserveStock), new { id = reservation.Id }, reservation);
            }
            catch (InsufficientStockException ex)
            {
                _logger.LogWarning(ex, "Insufficient stock for reservation");
                return BadRequest(new
                {
                    error = "InsufficientStock",
                    message = ex.Message,
                    productId = ex.ProductId,
                    warehouseId = ex.WarehouseId,
                    requested = ex.RequestedQuantity,
                    available = ex.AvailableQuantity
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid stock reservation operation");
                return BadRequest(new { error = "InvalidOperation", message = ex.Message });
            }
        }

        [HttpDelete("reservations/{reservationId}")]
        [HasPermission("Inventory", "Update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReleaseReservation(Guid reservationId)
        {
            try
            {
                await _warehouseStockService.ReleaseReservationAsync(reservationId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Reservation not found: {@Error}", new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpPost("adjust")]
        [HasPermission("Inventory", "Update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
        {
            try
            {
                await _warehouseStockService.AdjustStockAsync(dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Stock adjustment failed");
                return BadRequest(new { error = "AdjustmentFailed", message = ex.Message });
            }
        }
    }
}
