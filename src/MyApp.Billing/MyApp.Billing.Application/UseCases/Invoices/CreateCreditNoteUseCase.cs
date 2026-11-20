using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Creates a credit note for an invoice.</summary>
public sealed class CreateCreditNoteUseCase(
    IInvoiceRepository invoiceRepository,
    ICreditNoteRepository creditNoteRepository,
    IBillingTransactionBoundary boundary)
{
    /// <summary>Creates and persists a credit note.</summary>
    public async Task<CreditNoteDto> ExecuteAsync(CreateCreditNoteDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(dto.InvoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice {dto.InvoiceId} not found");

        var lines = dto.Lines.Select(l => new CreditNoteLineData(
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRate,
            l.Discount)).ToList();

        var creditNote = invoice.CreateCreditNote(lines, dto.Reason);

        await creditNoteRepository.AddAsync(creditNote);
        await boundary.SaveChangesAsync(cancellationToken);

        await boundary.EventPublisher.PublishAsync(
            MessagingConstants.Topics.BillingCreditNoteIssued,
            new CreditNoteIssuedEvent(
                creditNote.Id,
                creditNote.OriginalInvoiceId,
                $"CN-{creditNote.Id.ToString()[..8]}",
                creditNote.TotalGross,
                creditNote.Reason),
            cancellationToken);

        return BillingInvoiceMapper.ToDto(creditNote);
    }
}