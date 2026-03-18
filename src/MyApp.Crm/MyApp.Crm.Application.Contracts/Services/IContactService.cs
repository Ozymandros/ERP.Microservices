using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Crm.Application.Contracts.Services;

public interface IContactService
{
    Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContactDto>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<ContactDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default);

    Task<ContactDto> CreateAsync(CreateContactDto dto, CancellationToken cancellationToken = default);
    Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, CancellationToken cancellationToken = default);
    Task SetPrimaryAsync(Guid accountId, Guid contactId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

