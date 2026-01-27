using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

public record PurchaseOrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public PurchaseOrderStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public SupplierDto? Supplier { get; init; }
    public List<PurchaseOrderLineDto> Lines { get; init; } = new();
}

public record PurchaseOrderLineDto
{
    public Guid Id { get; init; }
    public Guid PurchaseOrderId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public record CreateUpdatePurchaseOrderDto
{
    [Required(ErrorMessage = "SupplierId is required")]
    public Guid SupplierId { get; init; }

    [Required(ErrorMessage = "OrderDate is required")]
    public DateTime OrderDate { get; init; }

    public DateTime? ExpectedDeliveryDate { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Status must be a valid value")]
    public int Status { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
    public decimal TotalAmount { get; init; }

    public List<CreateUpdatePurchaseOrderLineDto> Lines { get; init; } = new();
}

public record CreateUpdatePurchaseOrderLineDto
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// DTO for approving a purchase order
/// </summary>
public record ApprovePurchaseOrderDto
{
    [Required]
    public Guid PurchaseOrderId { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for receiving a purchase order
/// </summary>
public record ReceivePurchaseOrderDto
{
    [Required]
    public Guid PurchaseOrderId { get; init; }

    [Required]
    public Guid WarehouseId { get; init; }

    [Required]
    public DateTime ReceivedDate { get; init; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; init; }

    public List<ReceivePurchaseOrderLineDto> Lines { get; init; } = new();
}

/// <summary>
/// DTO for receiving a purchase order line
/// </summary>
public record ReceivePurchaseOrderLineDto
{
    [Required]
    public Guid PurchaseOrderLineId { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Received quantity must be at least 1")]
    public int ReceivedQuantity { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}