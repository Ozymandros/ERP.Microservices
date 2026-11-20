using MyApp.Billing.Application.Contracts.DTOs;

namespace MyApp.Billing.Application.Contracts.Services;

/// <summary>Supplier (AP) invoice operations.</summary>
public interface ISupplierInvoiceService
{
    Task<SupplierInvoiceDto> CreateAsync(CreateSupplierInvoiceDto dto, CancellationToken cancellationToken = default);
    Task<SupplierInvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SupplierInvoiceDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
}
