using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Application.Mapping;

/// <summary>Maps Billing domain entities to application DTOs.</summary>
public static class BillingInvoiceMapper
{
    /// <summary>Maps an invoice aggregate to a read DTO.</summary>
    public static InvoiceDto ToDto(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.Currency,
            invoice.Status.ToString(),
            invoice.IssueDate,
            invoice.DueDate,
            invoice.TotalNet,
            invoice.TotalTax,
            invoice.TotalGross,
            invoice.OutstandingAmount,
            invoice.Lines.Select(l => new InvoiceLineDto(
                l.Id,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.Discount,
                l.TaxRate,
                l.LineNet,
                l.LineTax,
                l.LineGross)).ToList(),
            invoice.CreatedAt,
            invoice.UpdatedAt ?? invoice.CreatedAt,
            invoice.SourceType,
            invoice.SourceId);

    /// <summary>Maps a credit note to a read DTO.</summary>
    public static CreditNoteDto ToDto(CreditNote creditNote) =>
        new(
            creditNote.Id,
            creditNote.OriginalInvoiceId,
            creditNote.Reason,
            creditNote.Status.ToString(),
            creditNote.TotalNet,
            creditNote.TotalTax,
            creditNote.TotalGross,
            creditNote.CreatedAt);
}