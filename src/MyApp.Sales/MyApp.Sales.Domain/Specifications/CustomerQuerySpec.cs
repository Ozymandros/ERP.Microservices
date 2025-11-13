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

    public override IQueryable<Customer> Apply(IQueryable<Customer> query)
    {
        // Apply customer-specific filters
        if (Query.Filters?.TryGetValue(nameof(Customer.Name), out var nameFilter) == true)
            query = query.Where(c => c.Name.ToLower().Contains(nameFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Customer.Email), out var emailFilter) == true)
            query = query.Where(c => c.Email.ToLower().Contains(emailFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Customer.PhoneNumber), out var phoneFilter) == true)
            query = query.Where(c => c.PhoneNumber.ToLower().Contains(phoneFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Customer.Address), out var addressFilter) == true)
            query = query.Where(c => c.Address.ToLower().Contains(addressFilter.ToString()!.ToLower()));

        // Apply search (searches in name, email, phone, and address)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.PhoneNumber.ToLower().Contains(term) ||
                c.Address.ToLower().Contains(term)
            );
        }

        return ApplyPaginationAndSorting(query);
    }
}
