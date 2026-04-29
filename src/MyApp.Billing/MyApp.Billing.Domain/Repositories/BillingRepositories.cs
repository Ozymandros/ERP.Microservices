using MyApp.Billing.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Billing.Domain.Repositories;

/// <summary>
/// Repository interface for Invoice aggregate
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice, Guid>
{
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Payment entity
/// </summary>
public interface IPaymentRepository : IRepository<Payment, Guid>
{
    Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for CreditNote entity
/// </summary>
public interface ICreditNoteRepository : IRepository<CreditNote, Guid>
{
    Task<List<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
