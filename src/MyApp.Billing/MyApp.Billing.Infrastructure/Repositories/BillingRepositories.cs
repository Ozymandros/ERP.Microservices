using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Billing.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IInvoiceRepository
/// </summary>
public class InvoiceRepository : Repository<Invoice, Guid>, IInvoiceRepository
{
    public InvoiceRepository(BillingDbContext context) : base(context)
    {
    }

    private BillingDbContext BillingContext => (BillingDbContext)Context;

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await BillingContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await BillingContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await BillingContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Sent)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await BillingContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// EF Core implementation of IPaymentRepository
/// </summary>
public class PaymentRepository : Repository<Payment, Guid>, IPaymentRepository
{
    public PaymentRepository(BillingDbContext context) : base(context)
    {
    }

    private BillingDbContext BillingContext => (BillingDbContext)Context;

    public async Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await BillingContext.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// EF Core implementation of ICreditNoteRepository
/// </summary>
public class CreditNoteRepository : Repository<CreditNote, Guid>, ICreditNoteRepository
{
    public CreditNoteRepository(BillingDbContext context) : base(context)
    {
    }

    private BillingDbContext BillingContext => (BillingDbContext)Context;

    public async Task<List<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await BillingContext.CreditNotes
            .Include(cn => cn.Lines)
            .Where(cn => cn.OriginalInvoiceId == invoiceId)
            .ToListAsync(cancellationToken);
    }
}
