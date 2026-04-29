using System.ComponentModel.DataAnnotations;
using MyApp.Shared.Domain.DTOs;

namespace MyApp.Inventory.Application.Contracts.DTOs;

/// <summary>
/// Represents the Warehouse Stock Dto data record.
/// </summary>
public record WarehouseStockDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or sets Warehouse Id.</summary>
    public Guid WarehouseId { get; init; }
    /// <summary>Gets or sets Warehouse Name.</summary>
    public string? WarehouseName { get; init; }
    /// <summary>Gets or sets Available Quantity.</summary>
    public int AvailableQuantity { get; init; }
    /// <summary>Gets or sets Reserved Quantity.</summary>
    public int ReservedQuantity { get; init; }
    /// <summary>Gets or sets On Order Quantity.</summary>
    public int OnOrderQuantity { get; init; }
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;
}

/// <summary>
/// Represents the Reserve Stock Dto data record.
/// </summary>
public record ReserveStockDto
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or sets Warehouse Id.</summary>
    public Guid WarehouseId { get; init; }
    /// <summary>Gets or sets Quantity.</summary>
    public int Quantity { get; init; }
    /// <summary>Gets or sets Order Id.</summary>
    public Guid OrderId { get; init; }
    /// <summary>Gets or sets Order Line Id.</summary>
    public Guid? OrderLineId { get; init; }
    /// <summary>Gets or sets Expires At.</summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Represents the Stock Transfer Dto data record.
/// </summary>
public record StockTransferDto
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or sets From Warehouse Id.</summary>
    public Guid FromWarehouseId { get; init; }
    /// <summary>Gets or sets To Warehouse Id.</summary>
    public Guid ToWarehouseId { get; init; }
    /// <summary>Gets or sets Quantity.</summary>
    public int Quantity { get; init; }

    /// <summary>Gets or sets Reason.</summary>
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Represents the Stock Adjustment Dto data record.
/// </summary>
public record StockAdjustmentDto
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or sets Warehouse Id.</summary>
    public Guid WarehouseId { get; init; }
    /// <summary>Gets or sets Quantity Change.</summary>
    public int QuantityChange { get; init; }

    /// <summary>Gets or sets Reason.</summary>
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; init; } = string.Empty;

    /// <summary>Gets or sets Reference.</summary>
    [MaxLength(100, ErrorMessage = "Reference cannot exceed 100 characters")]
    public string? Reference { get; init; }
}

/// <summary>
/// Represents the Stock Availability Dto data record.
/// </summary>
public record StockAvailabilityDto
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or sets S K U.</summary>
    public string SKU { get; init; } = string.Empty;
    /// <summary>Gets or sets Product Name.</summary>
    public string ProductName { get; init; } = string.Empty;
    /// <summary>Gets or sets Total Available.</summary>
    public int TotalAvailable { get; init; }
    /// <summary>Gets or sets Total Reserved.</summary>
    public int TotalReserved { get; init; }
    /// <summary>Gets or sets Total On Order.</summary>
    public int TotalOnOrder { get; init; }
    /// <summary>Gets or sets Warehouse Stocks.</summary>
    public List<WarehouseStockDto> WarehouseStocks { get; init; } = new();
}

/// <summary>
/// Represents the Reservation Dto data record.
/// </summary>
public record ReservationDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or sets Warehouse Id.</summary>
    public Guid WarehouseId { get; init; }
    /// <summary>Gets or sets Order Id.</summary>
    public Guid OrderId { get; init; }
    /// <summary>Gets or sets Order Line Id.</summary>
    public Guid? OrderLineId { get; init; }
    /// <summary>Gets or sets Quantity.</summary>
    public int Quantity { get; init; }
    /// <summary>Gets or sets Reserved Until.</summary>
    public DateTime ReservedUntil { get; init; }
    /// <summary>Gets or sets Status.</summary>
    public string Status { get; init; } = string.Empty;
}
