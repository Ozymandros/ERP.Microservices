using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Billing.Infrastructure.Repositories;

/// <summary>
/// Repository for managing invoice entities in the database.
/// </summary>
public class InvoiceRepository : Repository<Invoice, Guid>, IInvoiceRepository
{
    /// <summary>
    /// Retrieves an invoice by its identifier with related lines and payments loaded.
    /// </summary>
    public override async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the InvoiceRepository with the provided database context.
    /// </summary>
    public InvoiceRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves an invoice by its invoice number.
    /// </summary>
    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    /// <summary>
    /// Retrieves all invoices for a specific customer, ordered by creation date descending.
    /// </summary>
    public async Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all outstanding invoices (issued or sent status), ordered by due date.
    /// </summary>
    public async Task<List<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Sent)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all invoices associated with a specific order.
    /// </summary>
    public async Task<List<Invoice>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Persists pending changes for tracked invoice aggregates.
    /// </summary>
    public new async Task<IReadOnlyCollection<EntityEntryDto>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When payments are appended through the Invoice aggregate, EF can occasionally
        // track new Payment rows as Modified instead of Added in this graph path.
        // Correct that state before SaveChanges to avoid false concurrency exceptions.
        var paymentEntries = _context.ChangeTracker.Entries<Payment>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in paymentEntries)
        {
            var paymentId = entry.Entity.Id;
            var exists = await _context.Payments
                .AsNoTracking()
                .AnyAsync(p => p.Id == paymentId, cancellationToken);

            if (!exists)
            {
                entry.State = EntityState.Added;
            }
        }

        return await base.SaveChangesAsync(disableTracking: false, cancellationToken);
    }

    /// <summary>
    /// Queries invoices using specification with lines included.
    /// </summary>
    public override async Task<PaginatedResult<Invoice>> QueryAsync(ISpecification<Invoice> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var baseQuery = _context.Invoices
            .Include(i => i.Lines)
            .AsNoTracking()
            .AsQueryable();

        var filteredQuery = spec.ApplyFilters(baseQuery);
        var totalCount = await filteredQuery.CountAsync();

        var finalQuery = spec.Apply(baseQuery);
        var items = await finalQuery.ToListAsync();

        var pageNumber = 1;
        var pageSize = items.Count;

        if (spec is BaseSpecification<Invoice> baseSpec)
        {
            pageNumber = baseSpec.Query.Page;
            pageSize = baseSpec.Query.PageSize;
        }

        return new PaginatedResult<Invoice>(items, pageNumber, pageSize, totalCount);
    }
}

/// <summary>
/// Repository for managing credit note entities in the database.
/// </summary>
public class CreditNoteRepository : Repository<CreditNote, Guid>, ICreditNoteRepository
{
    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the CreditNoteRepository with the provided database context.
    /// </summary>
    public CreditNoteRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all credit notes associated with a specific invoice.
    /// </summary>
    public async Task<List<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditNotes
            .Include(cn => cn.Lines)
            .Where(cn => cn.OriginalInvoiceId == invoiceId)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for managing payment entities in the database.
/// </summary>
public class PaymentRepository : Repository<Payment, Guid>, IPaymentRepository
{
    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the PaymentRepository with the provided database context.
    /// </summary>
    public PaymentRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all payments associated with a specific invoice, ordered by payment date descending.
    /// </summary>
    public async Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByExternalPaymentIdAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId, cancellationToken);
    }
}

