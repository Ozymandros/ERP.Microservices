using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Inventory.API.Controllers
{
    /// <summary>API controller for inventory transaction operations in the Inventory service.</summary>
    [ApiController]
    [Authorize]
    [Route("api/inventory/transactions")]
    public class InventoryTransactionsController : ControllerBase
    {
        private readonly IInventoryTransactionService _transactionService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<InventoryTransactionsController> _logger;

        /// <summary>Initialises a new instance of <see cref="InventoryTransactionsController"/>.</summary>
        /// Initializes a new instance of the InventoryTransactionsController class.
        /// <param name="transactionService">The transaction Service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cacheService">The cache Service.</param>
        public InventoryTransactionsController(IInventoryTransactionService transactionService, ILogger<InventoryTransactionsController> logger, ICacheService cacheService)
        {
            _transactionService = transactionService;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Export all inventory transactions as XLSX
        /// </summary>
        [HttpGet("export-xlsx")]
        [HasPermission("Inventory", "Read")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToXlsx()
        {
            try
            {
                var transactions = await _transactionService.GetAllTransactionsAsync();
                var bytes = transactions.ExportToXlsx();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryTransactions.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory transactions to XLSX");
                return StatusCode(500, new { message = "An error occurred exporting transactions" });
            }
        }

        /// <summary>
        /// Export all inventory transactions as PDF
        /// </summary>
        [HttpGet("export-pdf")]
        [HasPermission("Inventory", "Read")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToPdf()
        {
            try
            {
                var transactions = await _transactionService.GetAllTransactionsAsync();
                var bytes = transactions.ExportToPdf();
                return File(bytes, "application/pdf", "InventoryTransactions.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory transactions to PDF");
                return StatusCode(500, new { message = "An error occurred exporting transactions" });
            }
        }

        /// <summary>
        /// Get all inventory transactions
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
        /// <param name="pageNumber">The page Number.</param>
        /// <param name="pageSize">The page Size.</param>
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
        /// <param name="query">The query.</param>
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
                _logger.LogInformation("Searched transactions");
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
        /// <param name="id">The id.</param>
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
        /// Get transaction by Reference Number - Requires Inventory.Read permission
        /// </summary>
        /// <param name="referenceNumber">The reference Number.</param>
        [HttpGet("reference/{referenceNumber}")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InventoryTransactionDto>> GetTransactionByReferenceNumber(string referenceNumber)
        {
            try
            {
                string cacheKey = "Transaction-Reference-" + referenceNumber;
                var transaction = await _cacheService.GetStateAsync<InventoryTransactionDto>(cacheKey);

                if (transaction != null)
                {
                    _logger.LogInformation("Retrieved transaction with reference {@Reference} from cache", new { ReferenceNumber = referenceNumber });
                    return Ok(transaction);
                }

                transaction = await _transactionService.GetTransactionByReferenceNumberAsync(referenceNumber);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with reference {@Reference} not found", new { ReferenceNumber = referenceNumber });
                    return NotFound();
                }

                await _cacheService.SaveStateAsync(cacheKey, transaction);
                _logger.LogInformation("Retrieved transaction with reference {@Reference} from database and cached", new { ReferenceNumber = referenceNumber });
                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction with reference {@Reference}", new { ReferenceNumber = referenceNumber });
                var transaction = await _transactionService.GetTransactionByReferenceNumberAsync(referenceNumber);
                return transaction == null ? NotFound() : Ok(transaction);
            }
        }

        /// <summary>
        /// Get transactions by product ID - Requires Inventory.Read permission
        /// </summary>
        /// <param name="productId">The product Id.</param>
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
        /// <param name="warehouseId">The warehouse Id.</param>
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
        /// <param name="type">The type.</param>
        [HttpGet("type/{type}")]
        [HasPermission("Inventory", "Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<InventoryTransactionDto>>> GetTransactionsByType(string type)
        {
            if (!Enum.TryParse<TransactionType>(type, true, out var transactionType))
            {
                _logger.LogWarning("Invalid transaction type: {@Type}", new { Type = type });
                return BadRequest($"Invalid transaction type. Valid types are: {string.Join(", ", Enum.GetNames<TransactionType>())}");
            }
            _logger.LogInformation("Retrieving transactions of type: {@Type}", new { Type = type });
            var transactions = await _transactionService.GetTransactionsByTypeAsync(transactionType);
            return Ok(transactions);
        }

        /// <summary>
        /// Create a new inventory transaction - Requires Inventory.Create permission
        /// </summary>
        /// <param name="dto">The dto.</param>
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
        /// <param name="id">The id.</param>
        /// <param name="dto">The dto.</param>
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
        /// <param name="id">The id.</param>
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
}
