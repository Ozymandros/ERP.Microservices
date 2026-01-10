using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Inventory.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory/transactions")]
public class InventoryTransactionsController : ControllerBase
{
    private readonly IInventoryTransactionService _transactionService;
    private readonly ILogger<InventoryTransactionsController> _logger;

    public InventoryTransactionsController(IInventoryTransactionService transactionService, ILogger<InventoryTransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory transactions - Requires Inventory.Read permission
    /// </summary>
    [HttpGet]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetAllTransactions()
    {
        _logger.LogInformation("Retrieving all inventory transactions");
        var transactions = await _transactionService.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    /// <summary>
    /// Get all inventory transactions with pagination - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("paginated")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<InventoryTransactionDto>>> GetAllTransactionsPaginated([FromQuery(Name = "page")] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving paginated transactions: {@Pagination}", new { PageNumber = pageNumber, PageSize = pageSize });
            var result = await _transactionService.GetAllTransactionsPaginatedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated transactions");
            return StatusCode(500, new { message = "An error occurred retrieving transactions" });
        }
    }

    /// <summary>
    /// Search inventory transactions with advanced filtering, sorting, and pagination - Requires Inventory.Read permission
    /// </summary>
    /// <remarks>
    /// Supported filters: type, productId, warehouseId, minQuantity, maxQuantity
    /// Supported sort fields: id, transactionType, quantity, transactionDate, createdAt
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<InventoryTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<InventoryTransactionDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            query.Validate();
            var spec = new InventoryTransactionQuerySpec(query);
            var result = await _transactionService.QueryTransactionsAsync(spec);
            _logger.LogInformation("Searched transactions with query: {@Query}", query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transactions");
            return StatusCode(500, new { message = "An error occurred searching transactions" });
        }
    }

    /// <summary>
    /// Get transaction by ID - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryTransactionDto>> GetTransactionById(Guid id)
    {
        _logger.LogInformation("Retrieving transaction with ID: {@Transaction}", new { TransactionId = id });
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            _logger.LogWarning("Transaction with ID {@Transaction} not found", new { TransactionId = id });
            return NotFound();
        }
        return Ok(transaction);
    }

    /// <summary>
    /// Get transactions by product ID - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("product/{productId}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByProductId(Guid productId)
    {
        _logger.LogInformation("Retrieving transactions for product: {@Product}", new { ProductId = productId });
        var transactions = await _transactionService.GetTransactionsByProductIdAsync(productId);
        return Ok(transactions);
    }

    /// <summary>
    /// Get transactions by warehouse ID - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("warehouse/{warehouseId}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByWarehouseId(Guid warehouseId)
    {
        _logger.LogInformation("Retrieving transactions for warehouse: {@Warehouse}", new { WarehouseId = warehouseId });
        var transactions = await _transactionService.GetTransactionsByWarehouseIdAsync(warehouseId);
        return Ok(transactions);
    }

    /// <summary>
    /// Get transactions by type - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("type/{type}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByType(string type)
    {
        if (!Enum.TryParse<TransactionType>(type, true, out var transactionType))
        {
            _logger.LogWarning("Invalid transaction type: {@Type}", new { Type = type });
            return BadRequest($"Invalid transaction type. Valid types are: {string.Join(", ", Enum.GetNames(typeof(TransactionType)))}");
        }

        _logger.LogInformation("Retrieving transactions of type: {@Type}", new { Type = type });
        var transactions = await _transactionService.GetTransactionsByTypeAsync(transactionType);
        return Ok(transactions);
    }

    /// <summary>
    /// Create a new inventory transaction - Requires Inventory.Create permission
    /// </summary>
    [HttpPost]
    [HasPermission("Inventory", "Create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryTransactionDto>> CreateTransaction([FromBody] CreateUpdateInventoryTransactionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Creating new inventory transaction for product: {@Transaction}", new { ProductId = dto.ProductId });
            var transaction = await _transactionService.CreateTransactionAsync(dto);
            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, transaction);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict creating transaction: {@Error}", new { Message = ex.Message });
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing inventory transaction - Requires Inventory.Update permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Inventory", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryTransactionDto>> UpdateTransaction(Guid id, [FromBody] CreateUpdateInventoryTransactionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Updating transaction with ID: {@Transaction}", new { TransactionId = id });
            var transaction = await _transactionService.UpdateTransactionAsync(id, dto);
            return Ok(transaction);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict updating transaction: {@Error}", new { Message = ex.Message });
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Delete an inventory transaction - Requires Inventory.Delete permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("Inventory", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting transaction with ID: {@Transaction}", new { TransactionId = id });
            await _transactionService.DeleteTransactionAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }
}
