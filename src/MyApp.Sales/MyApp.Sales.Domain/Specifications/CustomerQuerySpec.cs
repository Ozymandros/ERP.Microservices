using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Domain.Specifications;

/// <summary>
/// Specification for querying customers with support for filtering, sorting, and pagination.
/// </summary>
public class CustomerQuerySpec : BaseSpecification<Customer>
{
    public CustomerQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Customer> ApplyFilters(IQueryable<Customer> query)
    {
        // Apply customer-specific filters
        if (Query.Filters?.TryGetValue(nameof(Customer.Name), out var nameFilter) == true && !string.IsNullOrEmpty(nameFilter))
            query = query.Where(c => c.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Customer.Email), out var emailFilter) == true && !string.IsNullOrEmpty(emailFilter))
            query = query.Where(c => c.Email.Contains(emailFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Customer.PhoneNumber), out var phoneFilter) == true && !string.IsNullOrEmpty(phoneFilter))
            query = query.Where(c => c.PhoneNumber.Contains(phoneFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Customer.Address), out var addressFilter) == true && !string.IsNullOrEmpty(addressFilter))
            query = query.Where(c => c.Address.Contains(addressFilter, StringComparison.OrdinalIgnoreCase));

        // Apply search (searches in name, email, phone, and address)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Address.Contains(term, StringComparison.OrdinalIgnoreCase)
            );
        }

        return query;
    }
}
