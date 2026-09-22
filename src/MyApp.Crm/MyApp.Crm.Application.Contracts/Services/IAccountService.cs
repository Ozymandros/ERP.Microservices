using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Crm.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Account Service.
/// </summary>
public interface IAccountService
{
    /// <summary>Gets an account by its unique identifier.</summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The account DTO, or null if not found.</returns>
    Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets an account by its associated customer ID.</summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The account DTO, or null if not found.</returns>
    Task<AccountDto?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Gets an account by its tax identification number.</summary>
    /// <param name="taxId">The tax ID to search for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The account DTO, or null if not found.</returns>
    Task<AccountDto?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default);

    /// <summary>Gets all accounts.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of all account DTOs.</returns>
    Task<IEnumerable<AccountDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Queries accounts using the specified filter and pagination specification.</summary>
    /// <param name="query">The query specification containing filters, sorting, and pagination.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated result of matching account DTOs.</returns>
    Task<PaginatedResult<AccountDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a CRM account from a sales snapshot.</summary>
    /// <param name="dto">The upsert data from the sales service.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created or updated account DTO.</returns>
    Task<AccountDto> UpsertFromSalesAsync(UpsertAccountDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates the owner of an account.</summary>
    /// <param name="id">The account ID.</param>
    /// <param name="dto">The DTO containing the new owner username.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated account DTO.</returns>
    Task<AccountDto> UpdateOwnerAsync(Guid id, UpdateAccountOwnerDto dto, CancellationToken cancellationToken = default);
}

