using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Domain.BusinessRules;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Inventory.Application.Services;

public class WarehouseStockService : IWarehouseStockService
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<WarehouseStockService> _logger;

    public WarehouseStockService(
        IWarehouseStockRepository warehouseStockRepository,
        IProductRepository productRepository,
        IInventoryTransactionRepository transactionRepository,
        IMapper mapper,
        ILogger<WarehouseStockService> logger)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _productRepository = productRepository;
        _transactionRepository = transactionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<WarehouseStockDto?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId)
    {
        var stock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(productId, warehouseId);
        return stock == null ? null : _mapper.Map<WarehouseStockDto>(stock);
    }

    public async Task<List<WarehouseStockDto>> GetByProductIdAsync(Guid productId)
    {
        var stocks = await _warehouseStockRepository.GetByProductIdAsync(productId);
        return _mapper.Map<List<WarehouseStockDto>>(stocks);
    }

    public async Task<List<WarehouseStockDto>> GetByWarehouseIdAsync(Guid warehouseId)
    {
        var stocks = await _warehouseStockRepository.GetByWarehouseIdAsync(warehouseId);
        return _mapper.Map<List<WarehouseStockDto>>(stocks);
    }

    public async Task<StockAvailabilityDto?> GetProductAvailabilityAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return null;
        }

        var stocks = await _warehouseStockRepository.GetByProductIdAsync(productId);
        
        return new StockAvailabilityDto
        {
            ProductId = productId,
            SKU = product.SKU,
            ProductName = product.Name,
            TotalAvailable = stocks.Sum(s => s.AvailableQuantity),
            TotalReserved = stocks.Sum(s => s.ReservedQuantity),
            TotalOnOrder = stocks.Sum(s => s.OnOrderQuantity),
            WarehouseStocks = _mapper.Map<List<WarehouseStockDto>>(stocks)
        };
    }

    public async Task<ReservationDto> ReserveStockAsync(ReserveStockDto dto)
    {
        _logger.LogInformation("Reserving stock: ProductId={ProductId}, WarehouseId={WarehouseId}, Quantity={Quantity}, OrderId={OrderId}",
            dto.ProductId, dto.WarehouseId, dto.Quantity, dto.OrderId);

        // Get or create warehouse stock
        var warehouseStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(dto.ProductId, dto.WarehouseId);
        if (warehouseStock == null)
        {
            throw new InvalidOperationException($"No stock record found for product {dto.ProductId} in warehouse {dto.WarehouseId}");
        }

        // Validate reservation
        if (!StockInvariants.CanReserveStock(warehouseStock.AvailableQuantity, dto.Quantity))
        {
            throw new InsufficientStockException(dto.ProductId, dto.WarehouseId, dto.Quantity, warehouseStock.AvailableQuantity);
        }

        // Update warehouse stock
        warehouseStock.AvailableQuantity -= dto.Quantity;
        warehouseStock.ReservedQuantity += dto.Quantity;
        await _warehouseStockRepository.UpdateAsync(warehouseStock);

        // Create reservation record (Note: This would be in Orders service, returning a DTO for now)
        var expiresAt = dto.ExpiresAt ?? ReservationInvariants.CalculateReservationExpiry();
        
        _logger.LogInformation("Stock reserved successfully: ProductId={ProductId}, Quantity={Quantity}, ExpiresAt={ExpiresAt}",
            dto.ProductId, dto.Quantity, expiresAt);

        // TODO: Publish StockReservedEvent via Dapr

        // Return reservation details (simplified for now)
        return new ReservationDto(Guid.NewGuid())
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            OrderId = dto.OrderId,
            OrderLineId = dto.OrderLineId,
            Quantity = dto.Quantity,
            ReservedUntil = expiresAt,
            Status = "Reserved"
        };
    }

    public async Task ReleaseReservationAsync(Guid reservationId)
    {
        _logger.LogInformation("Releasing reservation: ReservationId={ReservationId}", reservationId);

        // TODO: This would query Orders.ReservedStock
        // For now, log the operation
        _logger.LogInformation("Reservation released: ReservationId={ReservationId}", reservationId);

        // TODO: Publish StockReleasedEvent via Dapr
    }

    public async Task TransferStockAsync(StockTransferDto dto)
    {
        _logger.LogInformation("Transferring stock: ProductId={ProductId}, From={From}, To={To}, Quantity={Quantity}",
            dto.ProductId, dto.FromWarehouseId, dto.ToWarehouseId, dto.Quantity);

        // Get source warehouse stock
        var sourceStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(dto.ProductId, dto.FromWarehouseId);
        if (sourceStock == null || sourceStock.AvailableQuantity < dto.Quantity)
        {
            throw new StockTransferException(dto.ProductId, dto.FromWarehouseId, dto.ToWarehouseId,
                $"Insufficient stock in source warehouse. Available: {sourceStock?.AvailableQuantity ?? 0}, Requested: {dto.Quantity}");
        }

        // Get or create destination warehouse stock
        var destStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(dto.ProductId, dto.ToWarehouseId);
        if (destStock == null)
        {
            destStock = new WarehouseStock(Guid.NewGuid())
            {
                ProductId = dto.ProductId,
                WarehouseId = dto.ToWarehouseId,
                AvailableQuantity = 0,
                ReservedQuantity = 0,
                OnOrderQuantity = 0
            };
            await _warehouseStockRepository.AddAsync(destStock);
        }

        // Perform transfer
        sourceStock.AvailableQuantity -= dto.Quantity;
        destStock.AvailableQuantity += dto.Quantity;

        await _warehouseStockRepository.UpdateAsync(sourceStock);
        await _warehouseStockRepository.UpdateAsync(destStock);

        // Create transactions
        var outboundTx = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.FromWarehouseId,
            QuantityChange = -dto.Quantity,
            TransactionType = TransactionType.Outbound,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = $"TRANSFER-{dto.FromWarehouseId}-{dto.ToWarehouseId}"
        };

        var inboundTx = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.ToWarehouseId,
            QuantityChange = dto.Quantity,
            TransactionType = TransactionType.Inbound,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = $"TRANSFER-{dto.FromWarehouseId}-{dto.ToWarehouseId}"
        };

        await _transactionRepository.AddAsync(outboundTx);
        await _transactionRepository.AddAsync(inboundTx);

        _logger.LogInformation("Stock transferred successfully: ProductId={ProductId}, Quantity={Quantity}",
            dto.ProductId, dto.Quantity);

        // TODO: Publish StockTransferredEvent via Dapr
    }

    public async Task AdjustStockAsync(StockAdjustmentDto dto)
    {
        _logger.LogInformation("Adjusting stock: ProductId={ProductId}, WarehouseId={WarehouseId}, Change={Change}, Reason={Reason}",
            dto.ProductId, dto.WarehouseId, dto.QuantityChange, dto.Reason);

        var warehouseStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(dto.ProductId, dto.WarehouseId);
        if (warehouseStock == null)
        {
            throw new InvalidOperationException($"No stock record found for product {dto.ProductId} in warehouse {dto.WarehouseId}");
        }

        // Apply adjustment
        warehouseStock.AvailableQuantity += dto.QuantityChange;

        // Validate result
        if (warehouseStock.AvailableQuantity < 0)
        {
            throw new InvalidOperationException($"Adjustment would result in negative stock. Current: {warehouseStock.AvailableQuantity - dto.QuantityChange}, Change: {dto.QuantityChange}");
        }

        await _warehouseStockRepository.UpdateAsync(warehouseStock);

        // Create transaction
        var transaction = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            QuantityChange = dto.QuantityChange,
            TransactionType = TransactionType.Adjustment,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = dto.Reference
        };

        await _transactionRepository.AddAsync(transaction);

        _logger.LogInformation("Stock adjusted successfully: ProductId={ProductId}, NewQuantity={NewQuantity}",
            dto.ProductId, warehouseStock.AvailableQuantity);

        // TODO: Publish StockAdjustedEvent via Dapr
    }

    public async Task<List<WarehouseStockDto>> GetLowStockAsync()
    {
        var lowStocks = await _warehouseStockRepository.GetLowStockAsync();
        return _mapper.Map<List<WarehouseStockDto>>(lowStocks);
    }
}
