using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Inventory.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/inventory/warehouse-stocks")]
    public class WarehouseStocksController : ControllerBase
    {
        private readonly IWarehouseStockService _warehouseStockService;
        private readonly ILogger<WarehouseStocksController> _logger;

        public WarehouseStocksController(IWarehouseStockService warehouseStockService, ILogger<WarehouseStocksController> logger)
        {
            _warehouseStockService = warehouseStockService;
            _logger = logger;
        }

        [HttpGet("by-product-warehouse/{productId}/{warehouseId}")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(typeof(WarehouseStockDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByProductAndWarehouse(Guid productId, Guid warehouseId)
        {
            var stock = await _warehouseStockService.GetByProductAndWarehouseAsync(productId, warehouseId);
            if (stock == null)
            {
                return NotFound(new { message = $"No stock found for product {productId} in warehouse {warehouseId}" });
            }
            return Ok(stock);
        }

        [HttpGet("product/{productId}")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(typeof(List<WarehouseStockDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByProduct(Guid productId)
        {
            var stocks = await _warehouseStockService.GetByProductIdAsync(productId);
            return Ok(stocks);
        }

        [HttpGet("warehouse/{warehouseId}")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(typeof(List<WarehouseStockDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByWarehouse(Guid warehouseId)
        {
            var stocks = await _warehouseStockService.GetByWarehouseIdAsync(warehouseId);
            return Ok(stocks);
        }

        [HttpGet("availability/{productId}")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(typeof(StockAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductAvailability(Guid productId)
        {
            var availability = await _warehouseStockService.GetProductAvailabilityAsync(productId);
            if (availability == null)
            {
                return NotFound(new { message = $"Product {productId} not found" });
            }
            return Ok(availability);
        }

        [HttpGet("low-stock")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(typeof(List<WarehouseStockDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLowStock()
        {
            var lowStocks = await _warehouseStockService.GetLowStockAsync();
            return Ok(lowStocks);
        }
    }
}
