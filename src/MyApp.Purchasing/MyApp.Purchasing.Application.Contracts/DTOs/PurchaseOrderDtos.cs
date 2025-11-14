using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

public record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    DateTime OrderDate,
    DateTime? ExpectedDeliveryDate,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    SupplierDto? Supplier,
    List<PurchaseOrderLineDto> Lines
);

public record PurchaseOrderLineDto(
    Guid Id,
    Guid PurchaseOrderId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record CreateUpdatePurchaseOrderDto(
    [property: Required(ErrorMessage = "OrderNumber is required")]
    [property: StringLength(64, MinimumLength = 1)]
    string OrderNumber,

    [property: Required(ErrorMessage = "SupplierId is required")]
    Guid SupplierId,

    [property: Required(ErrorMessage = "OrderDate is required")]
    DateTime OrderDate,

    DateTime? ExpectedDeliveryDate,

    [property: Range(0, int.MaxValue, ErrorMessage = "Status must be a valid value")]
    int Status,

    [property: Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
    decimal TotalAmount,

    List<CreateUpdatePurchaseOrderLineDto> Lines
);

public record CreateUpdatePurchaseOrderLineDto(
    [property: Required]
    Guid ProductId,

    [property: Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    int Quantity,

    [property: Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    decimal UnitPrice
);
