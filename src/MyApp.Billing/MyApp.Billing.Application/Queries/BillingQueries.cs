using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Application.Queries;

/// <summary>
/// Gets the invoice by id query.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
public record GetInvoiceByIdQuery(Guid InvoiceId);

/// <summary>
/// Gets the invoices by customer id query.
/// </summary>
/// <param name="CustomerId">The customer Id.</param>
public record GetInvoicesByCustomerIdQuery(Guid CustomerId);

/// <summary>
/// Gets the open invoices query.
/// </summary>
public record GetOpenInvoicesQuery();

/// <summary>
/// Gets the invoices by order id query.
/// </summary>
/// <param name="OrderId">The order Id.</param>
public record GetInvoicesByOrderIdQuery(Guid OrderId);
