using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Crm.Application.Services;

/// <summary>
/// Provides Contact Service functionality.
/// </summary>
public sealed class ContactService : IContactService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        IAccountRepository accountRepository,
        IContactRepository contactRepository,
        IMapper mapper,
        ILogger<ContactService> logger)
    {
        _accountRepository = accountRepository;
        _contactRepository = contactRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Get By Id Async.</summary>
    public async Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _contactRepository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<ContactDto>(entity);
    }

    /// <summary>List By Account Async.</summary>
    public async Task<IEnumerable<ContactDto>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var list = await _contactRepository.ListByAccountAsync(accountId, cancellationToken);
        return _mapper.Map<IEnumerable<ContactDto>>(list);
    }

    /// <summary>Query Async.</summary>
    public async Task<PaginatedResult<ContactDto>> QueryAsync(QuerySpec query, CancellationToken cancellationToken = default)
    {
        var spec = new ContactQuerySpec(query);
        var result = await _contactRepository.QueryAsync(spec);
        var dtos = result.Items.Select(c => _mapper.Map<ContactDto>(c)).ToList();
        return new PaginatedResult<ContactDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Create Async.</summary>
    public async Task<ContactDto> CreateAsync(CreateContactDto dto, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(dto.AccountId);
        if (account is null) throw new KeyNotFoundException($"Account with ID {dto.AccountId} not found.");

        var contact = account.AddContact(
            Guid.NewGuid(),
            dto.FullName,
            dto.Email,
            dto.Phone,
            dto.Title,
            dto.IsPrimary);

        await _accountRepository.UpdateAsync(account);
        _logger.LogInformation("Created contact {ContactId} for Account {AccountId}", contact.Id, account.Id);

        return _mapper.Map<ContactDto>(contact);
    }

    /// <summary>Update Async.</summary>
    public async Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id);
        if (contact is null) throw new KeyNotFoundException($"Contact with ID {id} not found.");

        contact.Update(dto.FullName, dto.Email, dto.Phone, dto.Title);
        await _contactRepository.UpdateAsync(contact);

        return _mapper.Map<ContactDto>(contact);
    }

    /// <summary>Set Primary Async.</summary>
    public async Task SetPrimaryAsync(Guid accountId, Guid contactId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null) throw new KeyNotFoundException($"Account with ID {accountId} not found.");

        account.SetPrimaryContact(contactId);
        await _accountRepository.UpdateAsync(account);
    }

    /// <summary>Deactivate Async.</summary>
    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id);
        if (contact is null) return;

        contact.Deactivate();
        await _contactRepository.UpdateAsync(contact);
    }
}

