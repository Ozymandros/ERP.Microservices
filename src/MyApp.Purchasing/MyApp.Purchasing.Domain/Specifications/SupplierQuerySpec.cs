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

    public override IQueryable<Supplier> Apply(IQueryable<Supplier> query)
    {
        // Apply supplier-specific filters
        if (Query.Filters?.TryGetValue(nameof(Supplier.Name), out var nameFilter) == true)
            query = query.Where(s => s.Name.ToLower().Contains(nameFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Supplier.Email), out var emailFilter) == true)
            query = query.Where(s => s.Email.ToLower().Contains(emailFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Supplier.ContactName), out var contactNameFilter) == true)
            query = query.Where(s => s.ContactName.ToLower().Contains(contactNameFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Supplier.PhoneNumber), out var phoneFilter) == true)
            query = query.Where(s => s.PhoneNumber.ToLower().Contains(phoneFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Supplier.Address), out var addressFilter) == true)
            query = query.Where(s => s.Address.ToLower().Contains(addressFilter.ToString()!.ToLower()));

        // Apply search (searches in name, email, contact name, phone, and address)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.Email.ToLower().Contains(term) ||
                s.ContactName.ToLower().Contains(term) ||
                s.PhoneNumber.ToLower().Contains(term) ||
                s.Address.ToLower().Contains(term)
            );
        }

        return ApplyPaginationAndSorting(query);
    }
}
