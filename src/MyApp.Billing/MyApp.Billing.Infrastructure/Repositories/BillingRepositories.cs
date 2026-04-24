using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Billing.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice, Guid>, IInvoiceRepository
{
    private readonly BillingDbContext _context;

    public InvoiceRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Sent)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}

public class CreditNoteRepository : Repository<CreditNote, Guid>, ICreditNoteRepository
{
    private readonly BillingDbContext _context;

    public CreditNoteRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditNotes
            .Include(cn => cn.Lines)
            .Where(cn => cn.OriginalInvoiceId == invoiceId)
            .ToListAsync(cancellationToken);
    }
}

public class PaymentRepository : Repository<Payment, Guid>, IPaymentRepository
{
    private readonly BillingDbContext _context;

    public PaymentRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);
    }
}

