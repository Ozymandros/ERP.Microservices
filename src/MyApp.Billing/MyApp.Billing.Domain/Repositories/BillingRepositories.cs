using MyApp.Billing.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Billing.Domain.Repositories;

/// <summary>
/// Repository interface for Invoice aggregate
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice, Guid>
{
    /// <summary>Retrieves an invoice by its human-readable invoice number.</summary>
    /// <param name="invoiceNumber">The invoice number to search for.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The matching <see cref="Invoice"/>, or <see langword="null"/> if not found.</returns>
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all invoices associated with a specific customer.</summary>
    /// <param name="customerId">The identifier of the customer.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of invoices belonging to the specified customer.</returns>
    Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all invoices with an open (unpaid, non-cancelled) status.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of open invoices.</returns>
    Task<List<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves all invoices linked to a specific order.</summary>
    /// <param name="orderId">The identifier of the order.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of invoices associated with the specified order.</returns>
    Task<List<Invoice>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Payment entity
/// </summary>
public interface IPaymentRepository : IRepository<Payment, Guid>
{
    /// <summary>Retrieves all payments recorded against a specific invoice.</summary>
    /// <param name="invoiceId">The identifier of the invoice.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of payments associated with the specified invoice.</returns>
    Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a payment by its external payment provider identifier.</summary>
    /// <param name="externalPaymentId">The external payment identifier (e.g. a Stripe charge ID).</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The matching <see cref="Payment"/>, or <see langword="null"/> if not found.</returns>
    Task<Payment?> GetByExternalPaymentIdAsync(string externalPaymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for CreditNote entity
/// </summary>
public interface ICreditNoteRepository : IRepository<CreditNote, Guid>
{
    /// <summary>Retrieves all credit notes issued against a specific invoice.</summary>
    /// <param name="invoiceId">The identifier of the original invoice.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of credit notes associated with the specified invoice.</returns>
    Task<List<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
