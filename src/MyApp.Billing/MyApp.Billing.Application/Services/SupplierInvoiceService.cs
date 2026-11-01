using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Billing.Application.Services;

/// <summary>Manages supplier invoices for AP / 3-way match.</summary>
public class SupplierInvoiceService : AppServiceBase, ISupplierInvoiceService
{
    private readonly ISupplierInvoiceRepository _repository;

    public SupplierInvoiceService(
        ISupplierInvoiceRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<SupplierInvoiceService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Billing)
    {
        _repository = repository;
    }

    public async Task<SupplierInvoiceDto> CreateAsync(CreateSupplierInvoiceDto dto, CancellationToken cancellationToken = default)
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

        await _repository.AddAsync(entity);
        await SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<SupplierInvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<List<SupplierInvoiceDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByPurchaseOrderIdAsync(purchaseOrderId, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    private static SupplierInvoiceDto MapToDto(SupplierInvoice entity) =>
        new(
            entity.Id,
            entity.InvoiceNumber,
            entity.SupplierId,
            entity.PurchaseOrderId,
            entity.Currency,
            entity.Lines.Select(l => new SupplierInvoiceLineDto(l.Id, l.ProductId, l.Quantity, l.UnitPrice)).ToList(),
            entity.CreatedAt,
            entity.UpdatedAt ?? entity.CreatedAt);
}
