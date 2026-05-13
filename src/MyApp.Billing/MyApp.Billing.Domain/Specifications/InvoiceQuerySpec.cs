using MyApp.Billing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Billing.Domain.Specifications;

/// <summary>
/// Specification for querying invoices with filtering, sorting, and pagination.
/// </summary>
public class InvoiceQuerySpec : BaseSpecification<Invoice>
{
    public InvoiceQuerySpec(QuerySpec query) : base(query) { }

    public override IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> query)
    {
        if (Query.Filters?.TryGetValue(nameof(Invoice.InvoiceNumber), out var invoiceNumberFilter) == true
            && !string.IsNullOrWhiteSpace(invoiceNumberFilter))
        {
            var term = invoiceNumberFilter.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(term));
        }

        if (Query.Filters?.TryGetValue(nameof(Invoice.CustomerId), out var customerIdFilter) == true
            && Guid.TryParse(customerIdFilter, out var customerId))
        {
            query = query.Where(i => i.CustomerId == customerId);
        }

        if (Query.Filters?.TryGetValue(nameof(Invoice.OrderId), out var orderIdFilter) == true
            && Guid.TryParse(orderIdFilter, out var orderId))
        {
            query = query.Where(i => i.OrderId == orderId);
        }

        if (Query.Filters?.TryGetValue(nameof(Invoice.Currency), out var currencyFilter) == true
            && !string.IsNullOrWhiteSpace(currencyFilter))
        {
            var currency = currencyFilter.Trim().ToUpper();
            query = query.Where(i => i.Currency == currency);
        }

        if (Query.Filters?.TryGetValue(nameof(Invoice.Status), out var statusFilter) == true
            && Enum.TryParse<InvoiceStatus>(statusFilter, true, out var status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (Query.Filters?.TryGetValue("IssueDateFrom", out var issueDateFromFilter) == true
            && DateTime.TryParse(issueDateFromFilter, out var issueDateFrom))
        {
            query = query.Where(i => i.IssueDate.HasValue && i.IssueDate.Value >= issueDateFrom);
        }

        if (Query.Filters?.TryGetValue("IssueDateTo", out var issueDateToFilter) == true
            && DateTime.TryParse(issueDateToFilter, out var issueDateTo))
        {
            query = query.Where(i => i.IssueDate.HasValue && i.IssueDate.Value <= issueDateTo);
        }

        if (Query.Filters?.TryGetValue("DueDateFrom", out var dueDateFromFilter) == true
            && DateTime.TryParse(dueDateFromFilter, out var dueDateFrom))
        {
            query = query.Where(i => i.DueDate.HasValue && i.DueDate.Value >= dueDateFrom);
        }

        if (Query.Filters?.TryGetValue("DueDateTo", out var dueDateToFilter) == true
            && DateTime.TryParse(dueDateToFilter, out var dueDateTo))
        {
            query = query.Where(i => i.DueDate.HasValue && i.DueDate.Value <= dueDateTo);
        }

        if (Query.Filters?.TryGetValue("OutstandingAmountMin", out var outstandingMinFilter) == true
            && decimal.TryParse(outstandingMinFilter, out var outstandingMin))
        {
            query = query.Where(i => i.OutstandingAmount >= outstandingMin);
        }

        if (Query.Filters?.TryGetValue("OutstandingAmountMax", out var outstandingMaxFilter) == true
            && decimal.TryParse(outstandingMaxFilter, out var outstandingMax))
        {
            query = query.Where(i => i.OutstandingAmount <= outstandingMax);
        }

        if (Query.Filters?.TryGetValue("TotalGrossMin", out var totalGrossMinFilter) == true
            && decimal.TryParse(totalGrossMinFilter, out var totalGrossMin))
        {
            query = query.Where(i => i.TotalGross >= totalGrossMin);
        }

        if (Query.Filters?.TryGetValue("TotalGrossMax", out var totalGrossMaxFilter) == true
            && decimal.TryParse(totalGrossMaxFilter, out var totalGrossMax))
        {
            query = query.Where(i => i.TotalGross <= totalGrossMax);
        }

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var searchTerm = Query.SearchTerm.Trim().ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(searchTerm) ||
                i.Currency.ToLower().Contains(searchTerm) ||
                i.Status.ToString().ToLower().Contains(searchTerm));
        }

        return query;
    }
}
