using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Crm.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Contact Service.
/// </summary>
public interface IContactService
{
    /// <summary>Gets a contact by its unique identifier.</summary>
    /// <param name="id">The contact ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The contact DTO, or null if not found.</returns>
    Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all contacts for a specific account.</summary>
    /// <param name="accountId">The account ID to retrieve contacts for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of contact DTOs for the account.</returns>
    Task<IEnumerable<ContactDto>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>Queries contacts using the specified filter and pagination specification.</summary>
    /// <param name="query">The query specification containing filters, sorting, and pagination.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated result of matching contact DTOs.</returns>
    Task<PaginatedResult<ContactDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default);

    /// <summary>Creates a new contact for an account.</summary>
    /// <param name="dto">The data for the new contact.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created contact DTO.</returns>
    Task<ContactDto> CreateAsync(CreateContactDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing contact's information.</summary>
    /// <param name="id">The contact ID.</param>
    /// <param name="dto">The updated contact data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated contact DTO.</returns>
    Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, CancellationToken cancellationToken = default);

    /// <summary>Sets a contact as the primary contact for an account.</summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="contactId">The contact ID to set as primary.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SetPrimaryAsync(Guid accountId, Guid contactId, CancellationToken cancellationToken = default);

    /// <summary>Deactivates a contact.</summary>
    /// <param name="id">The contact ID to deactivate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

