using MyApp.Shared.Domain.DTOs;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record WarehouseStockDto(Guid Id) : AuditableGuidDto(Id)
{
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int OnOrderQuantity { get; init; }
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;
}

public record ReserveStockDto
{
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public int Quantity { get; init; }
    public Guid OrderId { get; init; }
    public Guid? OrderLineId { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record StockTransferDto
{
    public Guid ProductId { get; init; }
    public Guid FromWarehouseId { get; init; }
    public Guid ToWarehouseId { get; init; }
    public int Quantity { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record StockAdjustmentDto
{
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public int QuantityChange { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Reference { get; init; }
}

public record StockAvailabilityDto
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int TotalAvailable { get; init; }
    public int TotalReserved { get; init; }
    public int TotalOnOrder { get; init; }
    public List<WarehouseStockDto> WarehouseStocks { get; init; } = new();
}

public record ReservationDto(Guid Id) : AuditableGuidDto(Id)
{
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? OrderLineId { get; init; }
    public int Quantity { get; init; }
    public DateTime ReservedUntil { get; init; }
    public string Status { get; init; } = string.Empty;
}
