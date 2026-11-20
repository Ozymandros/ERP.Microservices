using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;

namespace MyApp.Billing.Application.UseCases.SupplierInvoices;

/// <summary>Creates a supplier (AP) invoice.</summary>
public sealed class CreateSupplierInvoiceUseCase(
    ISupplierInvoiceRepository repository,
    IBillingTransactionBoundary boundary)
{
    /// <summary>Creates and persists a supplier invoice.</summary>
    public async Task<SupplierInvoiceDto> ExecuteAsync(CreateSupplierInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new SupplierInvoice(Guid.NewGuid())
        {
            InvoiceNumber = dto.InvoiceNumber,
            SupplierId = dto.SupplierId,
            PurchaseOrderId = dto.PurchaseOrderId,
            Currency = dto.Currency,
            Lines = dto.Lines.Select(l => new SupplierInvoiceLine(Guid.NewGuid())
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        foreach (var line in entity.Lines)
            line.SupplierInvoiceId = entity.Id;

        await repository.AddAsync(entity);
        await boundary.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    private static SupplierInvoiceDto MapToDto(SupplierInvoice entity) =>
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