using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Inventory.API.Controllers;

/// <summary>
/// Stock operations (reserve, transfer, adjust)
/// </summary>
[ApiController]
[Authorize]
[Route("api/inventory/stock-operations")]
public class StockOperationsController : ControllerBase
{
    private readonly IWarehouseStockService _warehouseStockService;
    private readonly ILogger<StockOperationsController> _logger;

    public StockOperationsController(
        IWarehouseStockService warehouseStockService,
        ILogger<StockOperationsController> logger)
    {
        _warehouseStockService = warehouseStockService;
        _logger = logger;
    }

    /// <summary>
    /// Reserve stock for an order
    /// </summary>
    /// <param name="dto">Reservation details</param>
    /// <remarks>
    /// Reserves stock for an order. The reservation will expire after 24 hours if not fulfilled.
    /// </remarks>
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

    /// <summary>
    /// Release a stock reservation
    /// </summary>
    /// <param name="reservationId">Reservation ID</param>
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
        catch (InvalidReservationException ex)
        {
            _logger.LogWarning(ex, "Invalid reservation release");
            return NotFound(new { error = "InvalidReservation", message = ex.Message });
        }
    }

    /// <summary>
    /// Transfer stock between warehouses
    /// </summary>
    /// <param name="dto">Transfer details</param>
    /// <remarks>
    /// Moves stock from one warehouse to another. Creates both outbound and inbound transactions.
    /// </remarks>
    [HttpPost("transfer")]
    [HasPermission("Inventory", "Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferStock([FromBody] StockTransferDto dto)
    {
        try
        {
            await _warehouseStockService.TransferStockAsync(dto);
            return NoContent();
        }
        catch (StockTransferException ex)
        {
            _logger.LogWarning(ex, "Stock transfer failed");
            return BadRequest(new
            {
                error = "StockTransferFailed",
                message = ex.Message,
                productId = ex.ProductId,
                fromWarehouseId = ex.FromWarehouseId,
                toWarehouseId = ex.ToWarehouseId
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid stock transfer operation");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
    }

    /// <summary>
    /// Adjust stock quantity (for damage, loss, found items, etc.)
    /// </summary>
    /// <param name="dto">Adjustment details</param>
    /// <remarks>
    /// Adjusts stock quantity up or down. Requires a reason for audit purposes.
    /// </remarks>
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
