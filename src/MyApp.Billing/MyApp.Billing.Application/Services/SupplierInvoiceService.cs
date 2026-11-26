using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Application.UseCases.SupplierInvoices;
using MyApp.Billing.Domain.Repositories;

namespace MyApp.Billing.Application.Services;

/// <summary>Facade for supplier invoice operations.</summary>
public class SupplierInvoiceService(
    ISupplierInvoiceRepository repository,
    CreateSupplierInvoiceUseCase createSupplierInvoice) : ISupplierInvoiceService
{
    /// <inheritdoc />
    public Task<SupplierInvoiceDto> CreateAsync(CreateSupplierInvoiceDto dto, CancellationToken cancellationToken = default) =>
        createSupplierInvoice.ExecuteAsync(dto, cancellationToken);

    /// <inheritdoc />
    public async Task<SupplierInvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<List<SupplierInvoiceDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var entities = await repository.GetByPurchaseOrderIdAsync(purchaseOrderId, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    private static SupplierInvoiceDto MapToDto(Domain.Entities.SupplierInvoice entity) =>
        new(
            entity.Id,
            entity.InvoiceNumber,
            entity.SupplierId,
            entity.PurchaseOrderId,
            entity.Currency,
            entity.Lines.Select(l => new SupplierInvoiceLineDto(
                l.Id,
                l.ProductId,
                l.Quantity,
                l.UnitPrice)).ToList(),
            entity.CreatedAt,
            entity.UpdatedAt ?? entity.CreatedAt);
}