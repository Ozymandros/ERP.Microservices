using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Application.Queries;

/// <summary>
/// Query to get an invoice by ID
/// </summary>
public record GetInvoiceByIdQuery(Guid InvoiceId);

/// <summary>
/// Query to get invoices for a customer
/// </summary>
public record GetInvoicesByCustomerIdQuery(Guid CustomerId);

/// <summary>
/// Query to get open invoices
/// </summary>
public record GetOpenInvoicesQuery();

/// <summary>
/// Query to get invoices by order ID
/// </summary>
public record GetInvoicesByOrderIdQuery(Guid OrderId);
