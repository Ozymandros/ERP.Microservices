using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Creates a customer invoice from a project invoice request event.</summary>
public sealed class CreateFromProjectRequestUseCase(
    IInvoiceRepository invoiceRepository,
    CreateInvoiceUseCase createInvoice,
    IssueInvoiceUseCase issueInvoice,
    ILogger<CreateFromProjectRequestUseCase> logger)
{
    /// <inheritdoc />
    public async Task<InvoiceDto?> ExecuteAsync(
        ProjectInvoiceRequestedEvent @event,
        bool autoIssue,
        CancellationToken cancellationToken = default)
    {
        if (@event.Lines.Count == 0)
        {
            logger.LogWarning("Project invoice request {RequestId} has no lines; skipping", @event.RequestId);
            return null;
        }

        var existing = await invoiceRepository.GetBySourceAsync(DocumentSources.Project, @event.RequestId, cancellationToken);
        var current = existing.FirstOrDefault(i => i.Status != InvoiceStatus.Cancelled);
        if (current is not null)
        {
            logger.LogInformation(
                "Invoice {InvoiceId} already exists for project invoice request {RequestId}",
                current.Id, @event.RequestId);
            return BillingInvoiceMapper.ToDto(current);
        }

        var draftNumber = $"DRAFT-{@event.RequestId:N}"[..20];
        var createDto = new CreateInvoiceDto(
            draftNumber,
            @event.CustomerId,
            null,
            string.IsNullOrWhiteSpace(@event.Currency) ? "USD" : @event.Currency.ToUpperInvariant(),
            @event.Lines.Select(l => new CreateInvoiceLineDto(
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.TaxRate,
                l.Discount)).ToList(),
            SourceType: DocumentSources.Project,
            SourceId: @event.RequestId);

        var created = await createInvoice.ExecuteAsync(createDto, cancellationToken);

        var sanitizedProjectNumber = (@event.ProjectNumber ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);

        logger.LogInformation(
            "Created {Status} invoice {InvoiceId} for project {ProjectNumber} (request {RequestId})",
            autoIssue ? "auto-issued" : "draft", created.Id, sanitizedProjectNumber, @event.RequestId);

        if (!autoIssue)
            return created;

        var issueNumber = $"INV-{@event.ProjectNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (issueNumber.Length > 50)
            issueNumber = issueNumber[..50];
        return await issueInvoice.ExecuteAsync(created.Id, issueNumber, DateTime.UtcNow, cancellationToken);
    }
}