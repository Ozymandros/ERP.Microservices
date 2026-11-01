using System.ComponentModel.DataAnnotations;

namespace MyApp.Billing.Application.Contracts.DTOs;

/// <summary>Read model for a supplier invoice.</summary>
public record SupplierInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid SupplierId,
    Guid PurchaseOrderId,
    string Currency,
    List<SupplierInvoiceLineDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Line on a supplier invoice DTO.</summary>
public record SupplierInvoiceLineDto(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

/// <summary>Create supplier invoice.</summary>
public record CreateSupplierInvoiceDto(
    [Required] string InvoiceNumber,
    [Required] Guid SupplierId,
    [Required] Guid PurchaseOrderId,
    [Required] string Currency,
    [Required] List<CreateSupplierInvoiceLineDto> Lines);

/// <summary>Create supplier invoice line.</summary>
public record CreateSupplierInvoiceLineDto(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice);
