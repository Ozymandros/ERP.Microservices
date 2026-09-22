using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

/// <summary>Read DTO representing a purchase order and its lines.</summary>
public record PurchaseOrderDto
{
    /// <summary>Gets the unique identifier of the purchase order.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; init; } = string.Empty;
    /// <summary>Gets the unique identifier of the supplier.</summary>
    public Guid SupplierId { get; init; }
    /// <summary>Gets the date the order was placed.</summary>
    public DateTime OrderDate { get; init; }
    /// <summary>Gets the expected delivery date, if specified.</summary>
    public DateTime? ExpectedDeliveryDate { get; init; }
    /// <summary>Gets the current status of the purchase order.</summary>
    public PurchaseOrderStatus Status { get; init; }
    /// <summary>Gets the total monetary amount of the purchase order.</summary>
    public decimal TotalAmount { get; init; }
    /// <summary>Gets the navigated supplier details, if loaded.</summary>
    public SupplierDto? Supplier { get; init; }
    /// <summary>Gets the collection of order lines.</summary>
    public List<PurchaseOrderLineDto> Lines { get; init; } = new();
}

/// <summary>Read DTO representing a single line within a purchase order.</summary>
public record PurchaseOrderLineDto
{
    /// <summary>Gets the unique identifier of the order line.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the unique identifier of the parent purchase order.</summary>
    public Guid PurchaseOrderId { get; init; }
    /// <summary>Gets the unique identifier of the product being ordered.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets the quantity ordered.</summary>
    public int Quantity { get; init; }
    /// <summary>Gets the unit price agreed with the supplier.</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Gets the total cost for this line (Quantity * UnitPrice).</summary>
    public decimal LineTotal { get; init; }
}

/// <summary>DTO used to create or update a purchase order.</summary>
public record CreateUpdatePurchaseOrderDto
{
    /// <summary>Gets the unique identifier of the supplier for this order.</summary>
    [Required(ErrorMessage = "SupplierId is required")]
    public Guid SupplierId { get; init; }

    /// <summary>Gets the date the order is placed.</summary>
    [Required(ErrorMessage = "OrderDate is required")]
    public DateTime OrderDate { get; init; }

    /// <summary>Gets the expected delivery date, if known.</summary>
    public DateTime? ExpectedDeliveryDate { get; init; }

    /// <summary>Gets the integer representation of the purchase order status.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "Status must be a valid value")]
    public int Status { get; init; }

    /// <summary>Gets the total monetary value of the order.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
    public decimal TotalAmount { get; init; }

    /// <summary>Gets the collection of order lines.</summary>
    public List<CreateUpdatePurchaseOrderLineDto> Lines { get; init; } = new();
}

/// <summary>DTO used to create or update a single purchase order line.</summary>
public record CreateUpdatePurchaseOrderLineDto
{
    /// <summary>Gets the unique identifier of the product being ordered.</summary>
    [Required]
    public Guid ProductId { get; init; }

    /// <summary>Gets the quantity to order.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }

    /// <summary>Gets the unit price agreed with the supplier.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    public decimal UnitPrice { get; init; }
}

/// <summary>DTO used to approve a purchase order.</summary>
public record ApprovePurchaseOrderDto
{
    /// <summary>Gets the unique identifier of the purchase order to approve.</summary>
    [Required]
    public Guid PurchaseOrderId { get; init; }

    /// <summary>Gets optional approval notes.</summary>
    [MaxLength(500)]
    public string? Notes { get; init; }
}

/// <summary>DTO used to record the receipt of goods for a purchase order.</summary>
public record ReceivePurchaseOrderDto
{
    /// <summary>Gets the unique identifier of the purchase order being received.</summary>
    [Required]
    public Guid PurchaseOrderId { get; init; }

    /// <summary>Gets the unique identifier of the warehouse where goods are received.</summary>
    [Required]
    public Guid WarehouseId { get; init; }

    /// <summary>Gets the date and time the goods were received.</summary>
    [Required]
    public DateTime ReceivedDate { get; init; } = DateTime.UtcNow;

    /// <summary>Gets optional receiving notes.</summary>
    [MaxLength(500)]
    public string? Notes { get; init; }

    /// <summary>Gets the per-line receiving details.</summary>
    public List<ReceivePurchaseOrderLineDto> Lines { get; init; } = new();
}

/// <summary>DTO that captures the received quantity for a single purchase order line.</summary>
public record ReceivePurchaseOrderLineDto
{
    /// <summary>Gets the unique identifier of the purchase order line being received.</summary>
    [Required]
    public Guid PurchaseOrderLineId { get; init; }

    /// <summary>Gets the quantity of goods actually received for this line.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Received quantity must be at least 1")]
    public int ReceivedQuantity { get; init; }

    /// <summary>Gets optional notes for this line receipt.</summary>
    [MaxLength(500)]
    public string? Notes { get; init; }
}