using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

public record PurchaseOrderDto
{
    public PurchaseOrderDto() { }
    
    public PurchaseOrderDto(
        Guid id,
        string orderNumber,
        Guid supplierId,
        DateTime orderDate,
        DateTime? expectedDeliveryDate,
        PurchaseOrderStatus status,
        decimal totalAmount,
        SupplierDto? supplier,
        List<PurchaseOrderLineDto> lines)
    {
        Id = id;
        OrderNumber = orderNumber;
        SupplierId = supplierId;
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        Status = status;
        TotalAmount = totalAmount;
        Supplier = supplier;
        Lines = lines;
    }
    
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public SupplierDto? Supplier { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public record PurchaseOrderLineDto
{
    public PurchaseOrderLineDto() { }
    
    public PurchaseOrderLineDto(
        Guid id,
        Guid purchaseOrderId,
        Guid productId,
        int quantity,
        decimal unitPrice,
        decimal lineTotal)
    {
        Id = id;
        PurchaseOrderId = purchaseOrderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }
    
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public record CreateUpdatePurchaseOrderDto
{
    public CreateUpdatePurchaseOrderDto() { }
    
    public CreateUpdatePurchaseOrderDto(
        string orderNumber,
        Guid supplierId,
        DateTime orderDate,
        DateTime? expectedDeliveryDate,
        int status,
        decimal totalAmount,
        List<CreateUpdatePurchaseOrderLineDto> lines)
    {
        OrderNumber = orderNumber;
        SupplierId = supplierId;
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        Status = status;
        TotalAmount = totalAmount;
        Lines = lines;
    }
    
    [Required(ErrorMessage = "OrderNumber is required")]
    [StringLength(64, MinimumLength = 1)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "SupplierId is required")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "OrderDate is required")]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Status must be a valid value")]
    public int Status { get; set; } = 0;

    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
    public decimal TotalAmount { get; set; } = 0;

    public List<CreateUpdatePurchaseOrderLineDto> Lines { get; set; } = new();
}

public record CreateUpdatePurchaseOrderLineDto
{
    public CreateUpdatePurchaseOrderLineDto() { }
    
    public CreateUpdatePurchaseOrderLineDto(
        Guid productId,
        int quantity,
        decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    public decimal UnitPrice { get; set; }
}
