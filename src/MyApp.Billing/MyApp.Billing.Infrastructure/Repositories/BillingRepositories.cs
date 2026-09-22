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
    /// Gets an item by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    public override async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the InvoiceRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public InvoiceRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the invoice number asynchronously.
    /// </summary>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    /// <summary>
    /// Gets the customer id asynchronously.
    /// </summary>
    /// <param name="customerId">The customer Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the open invoices asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Sent)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the invoices by order id asynchronously.
    /// </summary>
    /// <param name="orderId">The order Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<Invoice>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Query asynchronously.
    /// </summary>
    /// <param name="spec">The spec.</param>
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
    /// Initializes a new instance of the CreditNoteRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public CreditNoteRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the invoice id asynchronously.
    /// </summary>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
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
    /// Initializes a new instance of the PaymentRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public PaymentRepository(BillingDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the invoice id asynchronously.
    /// </summary>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the external payment id asynchronously.
    /// </summary>
    /// <param name="externalPaymentId">The external Payment Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<Payment?> GetByExternalPaymentIdAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId, cancellationToken);
    }
}

