using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Domain.Specifications;

/// <summary>
/// Specification for querying suppliers with support for filtering, sorting, and pagination.
/// </summary>
public class SupplierQuerySpec : BaseSpecification<Supplier>
{
    public SupplierQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Supplier> ApplyFilters(IQueryable<Supplier> query)
    {
        // Apply supplier-specific filters
        if (Query.Filters?.TryGetValue(nameof(Supplier.Name), out var nameFilter) == true && !string.IsNullOrEmpty(nameFilter))
            query = query.Where(s => s.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Supplier.Email), out var emailFilter) == true && !string.IsNullOrEmpty(emailFilter))
            query = query.Where(s => s.Email.Contains(emailFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Supplier.ContactName), out var contactNameFilter) == true && !string.IsNullOrEmpty(contactNameFilter))
            query = query.Where(s => s.ContactName.Contains(contactNameFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Supplier.PhoneNumber), out var phoneFilter) == true && !string.IsNullOrEmpty(phoneFilter))
            query = query.Where(s => s.PhoneNumber.Contains(phoneFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Supplier.Address), out var addressFilter) == true && !string.IsNullOrEmpty(addressFilter))
            query = query.Where(s => s.Address.Contains(addressFilter, StringComparison.OrdinalIgnoreCase));

        // Apply search (searches in name, email, contact name, phone, and address)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(s =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.ContactName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Address.Contains(term, StringComparison.OrdinalIgnoreCase)
            );
        }

        return query;
    }
}
