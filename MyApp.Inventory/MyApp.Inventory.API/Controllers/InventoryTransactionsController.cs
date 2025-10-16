using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;

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
    /// Get all inventory transactions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetAllTransactions()
    {
        _logger.LogInformation("Retrieving all inventory transactions");
        var transactions = await _transactionService.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryTransactionDto>> GetTransactionById(Guid id)
    {
        _logger.LogInformation("Retrieving transaction with ID: {TransactionId}", id);
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            _logger.LogWarning("Transaction with ID {TransactionId} not found", id);
            return NotFound();
        }
        return Ok(transaction);
    }

    /// <summary>
    /// Get transactions by product ID
    /// </summary>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByProductId(Guid productId)
    {
        _logger.LogInformation("Retrieving transactions for product: {ProductId}", productId);
        var transactions = await _transactionService.GetTransactionsByProductIdAsync(productId);
        return Ok(transactions);
    }

    /// <summary>
    /// Get transactions by warehouse ID
    /// </summary>
    [HttpGet("warehouse/{warehouseId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByWarehouseId(Guid warehouseId)
    {
        _logger.LogInformation("Retrieving transactions for warehouse: {WarehouseId}", warehouseId);
        var transactions = await _transactionService.GetTransactionsByWarehouseIdAsync(warehouseId);
        return Ok(transactions);
    }

    /// <summary>
    /// Get transactions by type
    /// </summary>
    [HttpGet("type/{type}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByType(string type)
    {
        if (!Enum.TryParse<TransactionType>(type, true, out var transactionType))
        {
            _logger.LogWarning("Invalid transaction type: {Type}", type);
            return BadRequest($"Invalid transaction type. Valid types are: {string.Join(", ", Enum.GetNames(typeof(TransactionType)))}");
        }

        _logger.LogInformation("Retrieving transactions of type: {Type}", type);
        var transactions = await _transactionService.GetTransactionsByTypeAsync(transactionType);
        return Ok(transactions);
    }

    /// <summary>
    /// Create a new inventory transaction
    /// </summary>
    [HttpPost]
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
            _logger.LogInformation("Creating new inventory transaction for product: {ProductId}", dto.ProductId);
            var transaction = await _transactionService.CreateTransactionAsync(dto);
            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, transaction);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict creating transaction: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing inventory transaction
    /// </summary>
    [HttpPut("{id}")]
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
            _logger.LogInformation("Updating transaction with ID: {TransactionId}", id);
            var transaction = await _transactionService.UpdateTransactionAsync(id, dto);
            return Ok(transaction);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict updating transaction: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Delete an inventory transaction
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting transaction with ID: {TransactionId}", id);
            await _transactionService.DeleteTransactionAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Transaction not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }
}
