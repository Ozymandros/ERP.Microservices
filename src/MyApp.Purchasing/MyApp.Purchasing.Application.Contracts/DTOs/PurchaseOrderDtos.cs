using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

public record PurchaseOrderDto
{
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
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public record CreateUpdatePurchaseOrderDto
{
    [Required(ErrorMessage = "OrderNumber is required")]
    [StringLength(64, MinimumLength = 1)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "SupplierId is required")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "OrderDate is required")]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Status must be a valid value")]
    public int Status { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
    public decimal TotalAmount { get; set; }

    public List<CreateUpdatePurchaseOrderLineDto> Lines { get; set; } = new();
}

public record CreateUpdatePurchaseOrderLineDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    public decimal UnitPrice { get; set; }
}
