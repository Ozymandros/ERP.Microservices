using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Crm.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Account Service.
/// </summary>
public interface IAccountService
{
    Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResult<AccountDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default);

    Task<AccountDto> UpsertFromSalesAsync(UpsertAccountDto dto, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateOwnerAsync(Guid id, UpdateAccountOwnerDto dto, CancellationToken cancellationToken = default);
}

